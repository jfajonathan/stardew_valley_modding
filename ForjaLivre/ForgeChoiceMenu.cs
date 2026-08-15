using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace ForjaLivre
{
    /// <summary>Uma linha selecionável dentro do <see cref="ForgeChoiceMenu"/>.</summary>
    internal readonly struct ForgeChoiceRow
    {
        public readonly string Label;
        public readonly bool Enabled;
        public readonly Action OnSelect;

        public ForgeChoiceRow(string label, bool enabled, Action onSelect)
        {
            this.Label = label;
            this.Enabled = enabled;
            this.OnSelect = onSelect;
        }
    }

    /// <summary>Popup simples com uma lista de opções clicáveis, usado para o jogador escolher qual
    /// estatística ou encantamento aplicar. Abre como menu-filho da <c>ForgeMenu</c> (via
    /// <see cref="IClickableMenu.SetChildMenu"/>), então a Forja por baixo mantém seu estado intacto.</summary>
    internal sealed class ForgeChoiceMenu : IClickableMenu
    {
        private const int RowHeight = 64;
        private const int Padding = 32;

        private readonly string title;
        private readonly List<ForgeChoiceRow> rows;
        private readonly List<Rectangle> rowBounds = new();
        private int hoveredIndex = -1;

        public ForgeChoiceMenu(string title, List<ForgeChoiceRow> rows)
        {
            this.title = title;
            this.rows = rows;

            int menuWidth = 560;
            int menuHeight = 96 + RowHeight * rows.Count + Padding;
            int x = (Game1.uiViewport.Width - menuWidth) / 2;
            int y = (Game1.uiViewport.Height - menuHeight) / 2;

            this.initialize(x, y, menuWidth, menuHeight, showUpperRightCloseButton: true);

            for (int i = 0; i < rows.Count; i++)
            {
                this.rowBounds.Add(new Rectangle(
                    this.xPositionOnScreen + Padding,
                    this.yPositionOnScreen + 80 + i * RowHeight,
                    this.width - Padding * 2,
                    RowHeight - 8
                ));
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (this.upperRightCloseButton != null && this.upperRightCloseButton.containsPoint(x, y))
            {
                this.exitThisMenu();
                return;
            }

            for (int i = 0; i < this.rowBounds.Count; i++)
            {
                if (this.rows[i].Enabled && this.rowBounds[i].Contains(x, y))
                {
                    Game1.playSound("select");
                    this.rows[i].OnSelect();
                    this.exitThisMenu();
                    return;
                }
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            this.exitThisMenu();
        }

        public override void performHoverAction(int x, int y)
        {
            this.hoveredIndex = -1;
            for (int i = 0; i < this.rowBounds.Count; i++)
            {
                if (this.rows[i].Enabled && this.rowBounds[i].Contains(x, y))
                {
                    this.hoveredIndex = i;
                    break;
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);

            drawTextureBox(b, this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, Color.White);

            Utility.drawTextWithShadow(b, this.title, Game1.dialogueFont,
                new Vector2(this.xPositionOnScreen + Padding, this.yPositionOnScreen + 24), Game1.textColor);

            for (int i = 0; i < this.rowBounds.Count; i++)
            {
                Rectangle bounds = this.rowBounds[i];
                bool enabled = this.rows[i].Enabled;
                bool hovered = i == this.hoveredIndex;

                if (hovered && enabled)
                {
                    b.Draw(Game1.staminaRect, bounds, Color.Wheat * 0.4f);
                }

                Color color = !enabled ? Color.Gray : (hovered ? Color.DarkGoldenrod : Game1.textColor);
                Utility.drawTextWithShadow(b, this.rows[i].Label, Game1.smallFont,
                    new Vector2(bounds.X + 8, bounds.Y + 14), color);
            }

            this.upperRightCloseButton?.draw(b);
            this.drawMouse(b);
        }
    }
}
