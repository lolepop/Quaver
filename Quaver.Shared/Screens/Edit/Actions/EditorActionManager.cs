using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Quaver.API.Enums;
using Quaver.API.Maps;
using Quaver.API.Maps.Structures;
using Quaver.Shared.Screens.Edit.Actions.HitObjects;
using Quaver.Shared.Screens.Edit.Actions.HitObjects.Flip;
using Quaver.Shared.Screens.Edit.Actions.HitObjects.Move;
using Quaver.Shared.Screens.Edit.Actions.HitObjects.Place;
using Quaver.Shared.Screens.Edit.Actions.HitObjects.PlaceBatch;
using Quaver.Shared.Screens.Edit.Actions.HitObjects.Remove;
using Quaver.Shared.Screens.Edit.Actions.HitObjects.RemoveBatch;
using Quaver.Shared.Screens.Edit.Actions.HitObjects.Resize;
using Quaver.Shared.Screens.Edit.Actions.Hitsounds.Add;
using Quaver.Shared.Screens.Edit.Actions.Hitsounds.Remove;
using Quaver.Shared.Screens.Edit.Actions.Layers.Colors;
using Quaver.Shared.Screens.Edit.Actions.Layers.Create;
using Quaver.Shared.Screens.Edit.Actions.Layers.Remove;
using Quaver.Shared.Screens.Edit.Actions.Layers.Rename;
using Quaver.Shared.Screens.Edit.Actions.SV.Add;
using Quaver.Shared.Screens.Edit.Actions.SV.AddBatch;
using Quaver.Shared.Screens.Edit.Actions.SV.Remove;
using Quaver.Shared.Screens.Edit.Actions.SV.RemoveBatch;
using Quaver.Shared.Screens.Edit.Actions.Timing.Add;
using Quaver.Shared.Screens.Edit.Actions.Timing.AddBatch;
using Quaver.Shared.Screens.Edit.Actions.Timing.Remove;
using Quaver.Shared.Screens.Edit.Actions.Timing.RemoveBatch;
using Quaver.Shared.Screens.Edit.Components;

namespace Quaver.Shared.Screens.Edit.Actions
{
    public class EditorActionManager : IDisposable
    {
        /// <summary>
        /// </summary>
        private EditScreen EditScreen { get; }

        /// <summary>
        /// </summary>
        private Qua WorkingMap { get; }

        /// <summary>
        ///     Stores a LIFO structure of actions to undo.
        /// </summary>
        public Stack<IEditorAction> UndoStack { get; } = new Stack<IEditorAction>();

        /// <summary>
        ///     Stores a LIFO structure of actions to redo.
        /// </summary>
        public Stack<IEditorAction> RedoStack { get; } = new Stack<IEditorAction>();

        /// <summary>
        ///     The last action the user performed before saving
        /// </summary>
        public IEditorAction LastSaveAction { get; set; }

        /// <summary>
        ///    Detects if the user has made changes to the map before saving.
        /// </summary>
        public bool HasUnsavedChanges => UndoStack.Count != 0  && UndoStack.Peek() != LastSaveAction || UndoStack.Count == 0 && LastSaveAction != null;

        /// <summary>
        ///     An action manager dedicated for lua plugins
        /// </summary>
        public EditorPluginActionManager PluginActionManager { get; }

        /// <summary>
        ///     Event invoked when a HitObject has been placed
        /// </summary>
        public event EventHandler<EditorHitObjectPlacedEventArgs> HitObjectPlaced;

        /// <summary>
        ///     Event invoked when a HitObject has been removed
        /// </summary>
        public event EventHandler<EditorHitObjectRemovedEventArgs> HitObjectRemoved;

        /// <summary>
        ///     Event invoked when a long note has been resized
        /// </summary>
        public event EventHandler<EditorLongNoteResizedEventArgs> LongNoteResized;

        /// <summary>
        ///     Event invoked when a batch of hitobjects have been removed
        /// </summary>
        public event EventHandler<EditorHitObjectBatchRemovedEventArgs> HitObjectBatchRemoved;

        /// <summary>
        ///     Event invoked when a batch of hitobjects have been placed
        /// </summary>
        public event EventHandler<EditorHitObjectBatchPlacedEventArgs> HitObjectBatchPlaced;

        /// <summary>
        ///     Event invoked when a batch of hitobjects have been flipped
        /// </summary>
        public event EventHandler<EditorHitObjectsFlippedEventArgs> HitObjectsFlipped;

        /// <summary>
        ///     Event invoked when a batch of hitobjects have been moved
        /// </summary>
        public event EventHandler<EditorHitObjectsMovedEventArgs> HitObjectsMoved;

        /// <summary>
        ///     Event invoked when a hitsound has been added to a group of objects
        /// </summary>
        public event EventHandler<EditorHitsoundAddedEventArgs> HitsoundAdded;

        /// <summary>
        ///     Event invoked when a hitsound has been removed from a group of objects
        /// </summary>
        public event EventHandler<EditorHitSoundRemovedEventArgs> HitsoundRemoved;

        /// <summary>
        ///     Event invoked when a layer has been created
        /// </summary>
        public event EventHandler<EditorLayerCreatedEventArgs> LayerCreated;

        /// <summary>
        ///     Event invoked when a layer has been deleted
        /// </summary>
        public event EventHandler<EditorLayerRemovedEventArgs> LayerDeleted;

        /// <summary>
        ///     Event invoked when a layer has been renamed
        /// </summary>
        public event EventHandler<EditorLayerRenamedEventArgs> LayerRenamed;

        /// <summary>
        ///     Event invoked when a layer's color has been changed
        /// </summary>
        public event EventHandler<EditorLayerColorChangedEventArgs> LayerColorChanged;

        /// <summary>
        ///     Event invoked when a scroll velocity has been added to the map
        /// </summary>
        public event EventHandler<EditorScrollVelocityAddedEventArgs> ScrollVelocityAdded;

        /// <summary>
        ///     Event invoked when a scroll velocity has been removed from the map
        /// </summary>
        public event EventHandler<EditorScrollVelocityRemovedEventArgs> ScrollVelocityRemoved;

        /// <summary>
        ///     Event invoked when a batch of scroll velocities has been added to the map
        /// </summary>
        public event EventHandler<EditorScrollVelocityBatchAddedEventArgs> ScrollVelocityBatchAdded;

        /// <summary>
        ///     Event invoked when a batch of scroll velocities has been removed from the map
        /// </summary>
        public event EventHandler<EditorScrollVelocityBatchRemovedEventArgs> ScrollVelocityBatchRemoved;

        /// <summary>
        ///     Event invoked when a timing point has been added to the map
        /// </summary>
        public event EventHandler<EditorTimingPointAddedEventArgs> TimingPointAdded;

        /// <summary>
        ///     Event invoked when a timing point has been removed from the map
        /// </summary>
        public event EventHandler<EditorTimingPointAddedEventArgs> TimingPointRemoved;

        /// <summary>
        ///     Event invoked when a batch of timing points has been added to the map
        /// </summary>
        public event EventHandler<EditorTimingPointBatchAddedEventArgs> TimingPointBatchAdded;

        /// <summary>
        ///     Event invoked when a batch of timing points has been removed from the map
        /// </summary>
        public event EventHandler<EditorTimingPointBatchRemovedEventArgs> TimingPointBatchRemoved;

        /// <summary>
        /// </summary>
        /// <param name="screen"></param>
        /// <param name="workingMap"></param>
        public EditorActionManager(EditScreen screen, Qua workingMap)
        {
            EditScreen = screen;
            WorkingMap = workingMap;
            PluginActionManager = new EditorPluginActionManager(this);
        }

        /// <summary>
        ///     Performs a given action for the editor to take.
        /// </summary>
        /// <param name="action"></param>
        public void Perform(IEditorAction action)
        {
            action.Perform();
            UndoStack.Push(action);
            RedoStack.Clear();
        }

        /// <summary>
        ///     Undos the first action in the stack
        /// </summary>
        public void Undo()
        {
            if (UndoStack.Count == 0)
                return;

            var action = UndoStack.Pop();
            action.Undo();

            RedoStack.Push(action);
        }

        /// <summary>
        ///     Redos the first action in the stack
        /// </summary>
        public void Redo()
        {
            if (RedoStack.Count == 0)
                return;

            var action = RedoStack.Pop();
            action.Perform();

            UndoStack.Push(action);
        }

        /// <summary>
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="layer"></param>
        /// <param name="hitsounds"></param>
        public HitObjectInfo PlaceHitObject(int lane, int startTime, int endTime = 0, int layer = 0, HitSounds hitsounds = 0)
        {
            var hitObject = new HitObjectInfo
            {
                Lane = lane,
                StartTime = startTime,
                EndTime = endTime,
                EditorLayer = layer,
                HitSound = hitsounds
            };

            Perform(new EditorActionPlaceHitObject(this, WorkingMap, hitObject));

            return hitObject;
        }

        /// <summary>
        /// </summary>
        /// <param name="hitObjects"></param>
        public void PlaceHitObjectBatch(List<HitObjectInfo> hitObjects) => Perform(new EditorActionPlaceHitObjectBatch(this, WorkingMap, hitObjects));

        /// <summary>
        ///     Removes a HitObject from the map
        /// </summary>
        /// <param name="h"></param>
        public void RemoveHitObject(HitObjectInfo h) => Perform(new EditorActionRemoveHitObject(this, WorkingMap, h));

        /// <summary>
        ///     Resizes a hitobject/long note to a given time
        /// </summary>
        /// <param name="h"></param>
        /// <param name="time"></param>
        public void ResizeLongNote(HitObjectInfo h, int originalTime, int time)
            => Perform(new EditorActionResizeLongNote(this, WorkingMap, h, originalTime, time));

        /// <summary>
        ///     Places an sv down in the map
        /// </summary>
        /// <param name="sv"></param>
        public void PlaceScrollVelocity(SliderVelocityInfo sv) => Perform(new EditorActionAddScrollVelocity(this, WorkingMap, sv));

        /// <summary>
        ///     Places a batch of scroll velocities into the map
        /// </summary>
        /// <param name="svs"></param>
        public void PlaceScrollVelocityBatch(List<SliderVelocityInfo> svs) => Perform(new EditorActionAddScrollVelocityBatch(this, WorkingMap, svs));

        /// <summary>
        ///     Adds a timing point to the map
        /// </summary>
        /// <param name="tp"></param>
        public void PlaceTimingPoint(TimingPointInfo tp) => Perform(new EditorActionAddTimingPoint(this, WorkingMap, tp));

        /// <summary>
        ///     Removes a timing point from the map
        /// </summary>
        /// <param name="tp"></param>
        public void RemoveTimingPoint(TimingPointInfo tp) => Perform(new EditorActionRemoveTimingPoint(this, WorkingMap, tp));

        /// <summary>
        ///     Places a batch of timing points to the map
        /// </summary>
        /// <param name="tps"></param>
        public void PlaceTimingPointBatch(List<TimingPointInfo> tps) => Perform(new EditorActionAddTimingPointBatch(this, WorkingMap, tps));

        /// <summary>
        /// </summary>
        /// <param name="input"></param>
        public void GoToObjects(string input) => EditScreen.GoToObjects(input);

        /// <summary>
        ///     Detects the BPM of the map and returns the object instance
        /// </summary>
        /// <returns></returns>
        public EditorBpmDetector DetectBpm() => new EditorBpmDetector(EditScreen.Track);

        /// <summary>
        ///     Triggers an event of a specific action type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="args"></param>
        public void TriggerEvent(EditorActionType type, EventArgs args)
        {
            switch (type)
            {
                case EditorActionType.PlaceHitObject:
                    HitObjectPlaced?.Invoke(this, (EditorHitObjectPlacedEventArgs) args);
                    break;
                case EditorActionType.RemoveHitObject:
                    HitObjectRemoved?.Invoke(this, (EditorHitObjectRemovedEventArgs) args);
                    break;
                case EditorActionType.ResizeLongNote:
                    LongNoteResized?.Invoke(this, (EditorLongNoteResizedEventArgs) args);
                    break;
                case EditorActionType.RemoveHitObjectBatch:
                    HitObjectBatchRemoved?.Invoke(this, (EditorHitObjectBatchRemovedEventArgs) args);
                    break;
                case EditorActionType.PlaceHitObjectBatch:
                    HitObjectBatchPlaced?.Invoke(this, (EditorHitObjectBatchPlacedEventArgs) args);
                    break;
                case EditorActionType.FlipHitObjects:
                    HitObjectsFlipped?.Invoke(this, (EditorHitObjectsFlippedEventArgs) args);
                    break;
                case EditorActionType.MoveHitObjects:
                    HitObjectsMoved?.Invoke(this, (EditorHitObjectsMovedEventArgs) args);
                    break;
                case EditorActionType.AddHitsound:
                    HitsoundAdded?.Invoke(this, (EditorHitsoundAddedEventArgs) args);
                    break;
                case EditorActionType.RemoveHitsound:
                    HitsoundRemoved?.Invoke(this, (EditorHitSoundRemovedEventArgs) args);
                    break;
                case EditorActionType.CreateLayer:
                    LayerCreated?.Invoke(this, (EditorLayerCreatedEventArgs) args);
                    break;
                case EditorActionType.RemoveLayer:
                    LayerDeleted?.Invoke(this, (EditorLayerRemovedEventArgs) args);
                    break;
                case EditorActionType.RenameLayer:
                    LayerRenamed?.Invoke(this, (EditorLayerRenamedEventArgs) args);
                    break;
                case EditorActionType.ColorLayer:
                    LayerColorChanged?.Invoke(this, (EditorLayerColorChangedEventArgs) args);
                    break;
                case EditorActionType.AddScrollVelocity:
                    ScrollVelocityAdded?.Invoke(this, (EditorScrollVelocityAddedEventArgs) args);
                    break;
                case EditorActionType.RemoveScrollVelocity:
                    ScrollVelocityRemoved?.Invoke(this, (EditorScrollVelocityRemovedEventArgs) args);
                    break;
                case EditorActionType.AddScrollVelocityBatch:
                    ScrollVelocityBatchAdded?.Invoke(this, (EditorScrollVelocityBatchAddedEventArgs) args);
                    break;
                case EditorActionType.RemoveScrollVelocityBatch:
                    ScrollVelocityBatchRemoved?.Invoke(this, (EditorScrollVelocityBatchRemovedEventArgs) args);
                    break;
                case EditorActionType.AddTimingPoint:
                    TimingPointAdded?.Invoke(this, (EditorTimingPointAddedEventArgs) args);
                    break;
                case EditorActionType.RemoveTimingPoint:
                    TimingPointRemoved?.Invoke(this, (EditorTimingPointRemovedEventArgs) args);
                    break;
                case EditorActionType.AddTimingPointBatch:
                    TimingPointBatchAdded?.Invoke(this, (EditorTimingPointBatchAddedEventArgs) args);
                    break;
                case EditorActionType.RemoveTimingPointBatch:
                    TimingPointBatchRemoved?.Invoke(this, (EditorTimingPointBatchRemovedEventArgs) args);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void Dispose()
        {
            HitObjectPlaced = null;
            HitObjectRemoved = null;
            LongNoteResized = null;
            HitObjectBatchRemoved = null;
            HitObjectBatchPlaced = null;
            HitObjectsFlipped = null;
            HitObjectsMoved = null;
            HitsoundAdded = null;
            HitsoundRemoved = null;
            LayerCreated = null;
            LayerDeleted = null;
            LayerRenamed = null;
            LayerColorChanged = null;
            ScrollVelocityAdded = null;
            ScrollVelocityRemoved = null;
            ScrollVelocityBatchAdded = null;
            ScrollVelocityBatchRemoved = null;
            TimingPointAdded = null;
            TimingPointRemoved = null;
            TimingPointBatchAdded = null;
            TimingPointBatchRemoved = null;
        }
    }
}