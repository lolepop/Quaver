using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics.Menu.Border.Components;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Helpers;
using Quaver.Shared.Screens.Selection.UI.Mapsets;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Selection.UI.Borders.Footer
{
    public class IconTextButtonOnlineListing : IconTextButton
    {
        public IconTextButtonOnlineListing() : base(FontAwesome.Get(FontAwesomeIcon.fa_earth_globe),
            FontManager.GetWobbleFont(Fonts.LatoBlack),"Online Listing", (sender, args) =>
            {
                if (MapManager.Selected.Value == null)
                    return;

                if (MapManager.Selected.Value.MapId == -1)
                {
                    NotificationManager.Show(NotificationLevel.Error, "This map is not submitted online!");
                    return;
                }

                BrowserHelper.OpenURL($"https://quavergame.com/mapsets/map/{MapManager.Selected.Value.MapId}");
            })
        {
        }
    }
}