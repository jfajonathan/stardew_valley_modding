using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Menus;
using StardewValley.Tools;

namespace ForjaLivre.Patches
{
    /// <summary>Adiciona dois botões à <c>ForgeMenu</c> ("Escolher Estatística" e "Escolher Encantamento")
    /// que abrem um <see cref="ForgeChoiceMenu"/> como menu-filho, e desenha o estado da escolha atual.
    /// Os botões são registrados como <see cref="ClickableComponent"/> de verdade (não retângulos soltos)
    /// para que participem do sistema de navegação por controle/teclado do jogo e tenham efeito de hover
    /// igual aos outros botões da Forja. Só ficam ativos quando o slot esquerdo contém uma arma
    /// corpo-a-corpo (não foice).</summary>
    [HarmonyPatch(typeof(ForgeMenu))]
    internal static class ForgeMenuUiPatch
    {
        private const int ButtonWidth = 272;
        private const int ButtonHeight = 52;
        private const int ButtonGap = 12;
        private const int ButtonOffsetX = 20;
        private const int ButtonOffsetY = 196;

        // IDs altos e arbitrários para não colidir com os componentes nativos da ForgeMenu
        // (ingredientes/botões usam 994-998, anéis usam 110/111, inventário usa 0-35 etc).
        private const int StatButtonId = 90210;
        private const int EnchantButtonId = 90211;

        private const float HoverMaxScale = 1.08f;
        private const float HoverStep = 0.04f;

        private static ClickableComponent? _statButton;
        private static ClickableComponent? _enchantButton;

        /// <summary>Cria (se necessário) e reposiciona os dois componentes, e garante que estejam
        /// registrados na lista de navegação da menu atual.</summary>
        private static void EnsureButtons(ForgeMenu menu)
        {
            _statButton ??= new ClickableComponent(Rectangle.Empty, "forjalivre_stat") { myID = StatButtonId };
            _enchantButton ??= new ClickableComponent(Rectangle.Empty, "forjalivre_enchant") { myID = EnchantButtonId };

            _statButton.bounds = new Rectangle(
                menu.xPositionOnScreen + menu.width + ButtonOffsetX,
                menu.yPositionOnScreen + ButtonOffsetY,
                ButtonWidth, ButtonHeight);

            _enchantButton.bounds = new Rectangle(
                menu.xPositionOnScreen + menu.width + ButtonOffsetX,
                menu.yPositionOnScreen + ButtonOffsetY + ButtonHeight + ButtonGap,
                ButtonWidth, ButtonHeight);

            // Liga os dois botões entre si, para navegação por controle/teclado (D-pad / WASD com
            // "Snappy Menus" ativado).
            _statButton.downNeighborID = EnchantButtonId;
            _enchantButton.upNeighborID = StatButtonId;

            if (menu.allClickableComponents == null)
                return;

            if (!menu.allClickableComponents.Contains(_statButton))
                menu.allClickableComponents.Add(_statButton);
            if (!menu.allClickableComponents.Contains(_enchantButton))
                menu.allClickableComponents.Add(_enchantButton);

            // Em vez de adivinhar qual componente nativo específico o jogador vai usar para chegar
            // até aqui, fazemos qualquer "beco sem saída" à direita (rightNeighborID == -1) tentar
            // uma busca automática pelo componente mais próximo nessa direção — o que passa a
            // incluir nossos botões, já que agora fazem parte de allClickableComponents.
            foreach (ClickableComponent component in menu.allClickableComponents)
            {
                if (component == _statButton || component == _enchantButton)
                    continue;
                if (component.rightNeighborID == -1)
                    component.rightNeighborID = ClickableComponent.SNAP_AUTOMATIC;
            }

            _statButton.leftNeighborID = ClickableComponent.SNAP_AUTOMATIC;
            _enchantButton.leftNeighborID = ClickableComponent.SNAP_AUTOMATIC;
        }

        private static bool TryGetWeapon(ForgeMenu menu, out MeleeWeapon weapon)
        {
            if (menu.leftIngredientSpot?.item is MeleeWeapon w && !w.isScythe())
            {
                weapon = w;
                return true;
            }
            weapon = null!;
            return false;
        }

        [HarmonyPatch(nameof(ForgeMenu.performHoverAction))]
        [HarmonyPostfix]
        private static void PerformHoverActionPostfix(ForgeMenu __instance, int x, int y)
        {
            EnsureButtons(__instance);
            AnimateHover(_statButton!, x, y);
            AnimateHover(_enchantButton!, x, y);
        }

        private static void AnimateHover(ClickableComponent component, int x, int y)
        {
            if (component.bounds.Contains(x, y))
                component.scale = Math.Min(component.scale + HoverStep, HoverMaxScale);
            else
                component.scale = Math.Max(component.scale - HoverStep, 1f);
        }

        [HarmonyPatch(nameof(ForgeMenu.draw))]
        [HarmonyPostfix]
        private static void DrawPostfix(ForgeMenu __instance, SpriteBatch b)
        {
            EnsureButtons(__instance);

            // Invalida a escolha atual se a arma no slot esquerdo mudou (ou foi removida).
            if (!ReferenceEquals(__instance.leftIngredientSpot?.item, ForgeChoiceState.TargetWeapon))
                ForgeChoiceState.Clear();

            bool hasWeapon = TryGetWeapon(__instance, out _);

            DrawButton(b, _statButton!, hasWeapon, GetStatLabel());
            DrawButton(b, _enchantButton!, hasWeapon, GetEnchantLabel());

            // A ForgeMenu original já desenhou o cursor antes deste postfix rodar; sem redesenhá-lo
            // por cima dos nossos botões, o cursor ficava visualmente "atrás" deles.
            if (!Game1.options.hardwareCursor)
                __instance.drawMouse(b);
        }

        private static void DrawButton(SpriteBatch b, ClickableComponent button, bool enabled, string label)
        {
            // O box "cresce" levemente a partir do centro quando o mouse/controle está sobre ele,
            // dando o mesmo feedback visual de hover que os outros botões da Forja.
            int extraW = (int)(button.bounds.Width * (button.scale - 1f));
            int extraH = (int)(button.bounds.Height * (button.scale - 1f));
            Rectangle drawBounds = new Rectangle(
                button.bounds.X - extraW / 2, button.bounds.Y - extraH / 2,
                button.bounds.Width + extraW, button.bounds.Height + extraH);

            bool hovered = enabled && button.scale > 1f;
            Color tint = !enabled ? Color.Gray * 0.6f : (hovered ? Color.Wheat : Color.White);

            IClickableMenu.drawTextureBox(b, drawBounds.X, drawBounds.Y, drawBounds.Width, drawBounds.Height, tint);

            Color textColor = enabled ? Game1.textColor : Color.Gray;
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(drawBounds.X + 14, drawBounds.Y + drawBounds.Height / 2 - 10), textColor, 0.85f);
        }

        private static string GetStatLabel()
        {
            ITranslationHelper translation = ModEntry.Instance.Helper.Translation;
            return ForgeChoiceState.ChosenStatEnchantment != null
                ? translation.Get("button.stat.chosen", new { stat = GetLabel(ForgeOptions.StatOptions, ForgeChoiceState.ChosenStatEnchantment) })
                : translation.Get("button.stat.choose");
        }

        private static string GetEnchantLabel()
        {
            ITranslationHelper translation = ModEntry.Instance.Helper.Translation;
            return ForgeChoiceState.ChosenWeaponEnchantment != null
                ? translation.Get("button.enchant.chosen", new { enchant = GetLabel(ForgeOptions.WeaponEnchantOptions, ForgeChoiceState.ChosenWeaponEnchantment) })
                : translation.Get("button.enchant.choose");
        }

        [HarmonyPatch(nameof(ForgeMenu.receiveLeftClick))]
        [HarmonyPostfix]
        private static void ReceiveLeftClickPostfix(ForgeMenu __instance, int x, int y)
        {
            if (__instance.GetChildMenu() != null)
                return;

            if (!TryGetWeapon(__instance, out MeleeWeapon weapon))
                return;

            EnsureButtons(__instance);

            if (_statButton!.bounds.Contains(x, y))
            {
                Game1.playSound("smallSelect");
                ForgeChoiceState.TargetWeapon = weapon;
                __instance.SetChildMenu(BuildStatChoiceMenu(weapon));
            }
            else if (_enchantButton!.bounds.Contains(x, y))
            {
                Game1.playSound("smallSelect");
                ForgeChoiceState.TargetWeapon = weapon;
                __instance.SetChildMenu(BuildEnchantChoiceMenu(weapon));
            }
        }

        private static string GetLabel(ForgeOption[] options, Type type)
        {
            foreach (ForgeOption option in options)
            {
                if (option.EnchantmentType == type)
                    return ModEntry.Instance.Helper.Translation.Get(option.TranslationKey);
            }
            return type.Name;
        }

        private static ForgeChoiceMenu BuildStatChoiceMenu(MeleeWeapon weapon)
        {
            ITranslationHelper translation = ModEntry.Instance.Helper.Translation;
            var rows = new List<ForgeChoiceRow>();

            foreach (ForgeOption option in ForgeOptions.StatOptions)
            {
                int currentLevel = GetEnchantmentLevel(weapon, option.EnchantmentType);
                string label = translation.Get(option.TranslationKey);
                if (currentLevel > 0)
                    label += " " + translation.Get("row.level", new { level = currentLevel });

                Type type = option.EnchantmentType;
                rows.Add(new ForgeChoiceRow(label, enabled: true, onSelect: () =>
                {
                    ForgeChoiceState.ChosenStatEnchantment = type;
                }));
            }

            return new ForgeChoiceMenu(translation.Get("menu.stat.title"), rows);
        }

        private static ForgeChoiceMenu BuildEnchantChoiceMenu(MeleeWeapon weapon)
        {
            ITranslationHelper translation = ModEntry.Instance.Helper.Translation;
            var rows = new List<ForgeChoiceRow>();

            List<BaseEnchantment> available = BaseEnchantment.GetAvailableEnchantmentsForItem(weapon);

            foreach (ForgeOption option in ForgeOptions.WeaponEnchantOptions)
            {
                bool isCurrentlyApplied = weapon.enchantments.Any(e => e.GetType() == option.EnchantmentType);
                bool isAvailable = available.Any(e => e.GetType() == option.EnchantmentType);
                bool rowEnabled = isAvailable && !isCurrentlyApplied;

                string label = translation.Get(option.TranslationKey);
                if (isCurrentlyApplied)
                    label += " " + translation.Get("row.applied");
                else if (!isAvailable)
                    label += " " + translation.Get("row.unavailable");

                Type type = option.EnchantmentType;
                rows.Add(new ForgeChoiceRow(label, rowEnabled, onSelect: () =>
                {
                    ForgeChoiceState.ChosenWeaponEnchantment = type;
                }));
            }

            return new ForgeChoiceMenu(translation.Get("menu.enchant.title"), rows);
        }

        private static int GetEnchantmentLevel(MeleeWeapon weapon, Type type)
        {
            int level = 0;
            foreach (BaseEnchantment enchantment in weapon.enchantments)
            {
                if (enchantment.GetType() == type)
                    level += enchantment.GetLevel();
            }
            return level;
        }
    }
}
