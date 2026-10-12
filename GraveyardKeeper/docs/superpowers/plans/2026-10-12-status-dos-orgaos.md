# Status dos Órgãos — Plano de Implementação

> **Para agentes:** SUB-SKILL OBRIGATÓRIO: use superpowers:subagent-driven-development (recomendado) ou superpowers:executing-plans para implementar este plano tarefa por tarefa. Os passos usam checkbox (`- [ ]`) para acompanhamento.

**Objetivo:** criar um plugin BepInEx para o Graveyard Keeper que mostra, no tooltip (a caixa que aparece ao passar o mouse sobre o item), quantas caveiras vermelhas e brancas cada órgão dá. A informação aparece desde o início do jogo, sem mudar o empilhamento de nenhum item.

**Arquitetura:** o tooltip do jogo já tem uma seção de caveiras (`Item.GetItemBodyModificators()`), mas ela fica escondida até o jogador ter o parâmetro `p_cultist > 0` (ou `show_q_bparts > 0`, no caso de olho, língua, músculo e couro cabeludo). O plugin usa dois patches Harmony:
1. Um prefix em `Item.GetItemBodyModificators` que, para órgãos, ignora essa trava e devolve os ícones de caveira no mesmo formato do jogo.
2. Um postfix em `ItemDefinition.GetTooltipData` que acrescenta uma linha com os números ("Vermelhas: 4   Brancas: 1", com os ícones de caveira).

A lógica de quem conta como órgão e a formatação do texto ficam em código C# puro (`Core/`), coberto por testes xUnit.

**Stack:** C# (`net472`), BepInEx 5.4.23.5, HarmonyX 2.9, Unity 2020.3.17 Mono, xUnit em `net10.0` para a lógica pura.

**Spec:** pedido do usuário: "focar apenas os órgãos; mostrar os status do órgão, sem empilhar; uma caixa de diálogo no item com as caveiras vermelhas e brancas que o órgão dá; os demais itens ficam como estão, com limite 1 de pilha". Dados de apoio em [../../gyk_organs_dump.tsv](../../gyk_organs_dump.tsv).

## Global Constraints

- Jogo: Graveyard Keeper Steam, versão 1.407, build `22583570`, 64-bit, em `D:\SteamLibrary\steamapps\common\Graveyard Keeper`.
- Loader: BepInEx `5.4.23.5`, já instalado. Compila para `net472` e referencia as DLLs do jogo com `Private=False`.
- GUID: `jonathan.gyk.statusdosorgaos`. Nome: `Status dos Órgãos`. Versão inicial: `1.0.0`.
- **Nenhum `stack_count` é alterado.** O mod não toca em `ItemDefinition.stack_count`, em inventários nem no save.
- Pasta do projeto: `GraveyardKeeper/StatusDosOrgaos/`. Testes em `GraveyardKeeper/StatusDosOrgaos.Tests/`.
- Código em `Core/` não referencia `Assembly-CSharp`, `UnityEngine` nem `BepInEx`.
- Textos visíveis em pt-BR.

## Fatos levantados no código e nos dados do jogo

- O tooltip do inventário vem de `BaseItemCellGUI` (linha 237), que chama `ItemDefinition.GetTooltipData(Item item, bool full_detail = true)` (ItemDefinition.cs:521).
- `GetTooltipData` (linhas 567–580) chama `item.GetItemBodyModificators()`. Para `type == BodyUniversalPart`, mostra o texto que essa chamada devolve depois de um separador.
- `Item.GetItemBodyModificators()` (Item.cs:2416) devolve `""` quando `definition.show_q_hint.has_expression && !definition.show_q_hint.EvaluateBoolean()`. Quando a trava está liberada, monta `"+(rskull)(rskull)…, +(skull)…"`. `(rskull)` e `(skull)` são ícones de caveira vermelha e branca que o texto do jogo renderiza.
- As caveiras vêm de `Item.GetRedSkullsValue()` (`q_minus + param bp_minus_mod`) e `Item.GetWhiteSkullsValue()` (`q_plus + param bp_plus_mod`). Assim, órgãos melhorados no Organ Enhancer já saem com os valores certos.
- O id codifica a qualidade como `heart:heart_<vermelhas>_<brancas>`. Exemplo do dump: `heart:heart_4_1` tem `q_minus = 4` e `q_plus = 1`.
- Travas reais (dump do jogo):
  - `Ppar("p_cultist") > 0`: coração, cérebro e intestino (24 variações cada), `heart_dark`, `brain_dark`, `intestine_dark`, `lungs_dark`, e também carne, gordura, sangue, pele, osso, caveira e `organs`.
  - `Ppar("show_q_bparts") > 0`: olho, língua, músculo, couro cabeludo.
  - Não existe pulmão comum do tipo `BodyUniversalPart`: `lungs:lungs_1` tem tipo `None` e é ignorado; o único pulmão coberto é `lungs_dark`. Olho, língua, músculo e couro cabeludo são 0/0 (só a linha numérica aparece).
- `BodyCraftGUI` (linha 56) também chama `GetItemBodyModificators()`, mas só para itens de embalsamamento, que não são órgãos. O patch não muda esse caminho.
- `TechUnlock` (linha 316) chama `GetTooltipData(…, full_detail: false)`. A linha de números só é adicionada quando `full_detail == true`, para não poluir a tela de tecnologias.
- O construtor `BubbleWidgetTextData(string, UITextStyles.TextStyle, NGUIText.Alignment, int)` tem parâmetros opcionais com tipos do NGUI. Por isso o projeto precisa referenciar também `Assembly-CSharp-firstpass.dll`.

## O que conta como "órgão"

| Escopo (config `Escopo`) | Ids incluídos |
|---|---|
| `Orgaos` (padrão) | `type == BodyUniversalPart` e base do id (antes de `:`, sem `_dark`) em `heart`, `brain`, `intestine`, `lungs`, `eye`, `tongue`, `scalp`, `muscle` |
| `TodasPartesDoCorpo` | qualquer `type == BodyUniversalPart`, o que inclui carne, sangue, gordura, pele, osso e caveira |

## Estrutura de arquivos

```
GraveyardKeeper/
├── .gitignore                          (já existe: bin/ obj/ out/)
├── StatusDosOrgaos/
│   ├── StatusDosOrgaos.csproj          build net472 → BepInEx\plugins\StatusDosOrgaos
│   ├── Plugin.cs                       entrada BepInEx + config
│   ├── Core/
│   │   ├── OrganClassifier.cs          decide se um id/tipo é órgão (puro)
│   │   └── SkullFormatter.cs           ícones no formato do jogo + linha numérica (puro)
│   ├── Patches/
│   │   ├── BodyModificatorsPatch.cs    prefix Item.GetItemBodyModificators (tira a trava)
│   │   └── TooltipSummaryPatch.cs      postfix ItemDefinition.GetTooltipData (linha numérica)
│   ├── README.md
│   └── CHANGELOG.md
└── StatusDosOrgaos.Tests/
    ├── StatusDosOrgaos.Tests.csproj    net10.0 + xUnit, linka ../StatusDosOrgaos/Core/*.cs
    ├── OrganClassifierTests.cs
    └── SkullFormatterTests.cs
```

## Review Focus

1. **Órgão melhorado no Organ Enhancer** (ex.: coração 4/1 que ganhou +1 branca): o tooltip deve mostrar o valor real (4/2), não o do id. Teste: o patch usa `GetRedSkullsValue()`/`GetWhiteSkullsValue()`, mais o passo manual da Task 3.
2. **Órgão com caveiras negativas** (`brain:brain_-2_3`, `heart:heart_2_-1`): o sinal deve aparecer igual ao do jogo ("-" com ícones). Teste: `SkullFormatterTests.Icons_NegativeValuesUseMinusSign` (Task 1).
3. **Órgão 0/0** (`heart:heart_0_0`): os ícones ficam vazios, como no jogo, mas a linha numérica mostra "0 · 0" para deixar claro que não dá caveiras. Teste: `SkullFormatterTests.Icons_ZeroZeroIsEmpty` e `Summary_ShowsZeros` (Task 1).
4. **Jogador que já liberou a trava (`p_cultist > 0`):** o tooltip não pode mostrar os ícones duas vezes. Teste: passo manual da Task 2. O prefix substitui o resultado em vez de concatenar, então só há uma linha de ícones.
5. **Itens que não são órgãos** (carne, ferramentas, orações, comida): o tooltip deve ficar idêntico ao original. Teste: `OrganClassifierTests.NonOrgans_AreRejected` (Task 1), mais o passo manual da Task 3.

---

### Task 1: Projetos, `OrganClassifier` e `SkullFormatter`

**Files:**
- Create: `GraveyardKeeper/StatusDosOrgaos/StatusDosOrgaos.csproj`
- Create: `GraveyardKeeper/StatusDosOrgaos/Core/OrganClassifier.cs`
- Create: `GraveyardKeeper/StatusDosOrgaos/Core/SkullFormatter.cs`
- Create: `GraveyardKeeper/StatusDosOrgaos.Tests/StatusDosOrgaos.Tests.csproj`
- Test: `GraveyardKeeper/StatusDosOrgaos.Tests/OrganClassifierTests.cs`
- Test: `GraveyardKeeper/StatusDosOrgaos.Tests/SkullFormatterTests.cs`

**Interfaces:**
- Produces:
  - `enum StatusDosOrgaos.Core.OrganScope { Orgaos, TodasPartesDoCorpo }`
  - `static bool OrganClassifier.IsOrgan(string id, string itemType, OrganScope scope)`
  - `static string SkullFormatter.Icons(int red, int white)`: mesmo formato de `Item.GetItemBodyModificators()`
  - `static string SkullFormatter.Summary(int red, int white)`: retorna `"(rskull) Vermelhas: {red}   (skull) Brancas: {white}"`

- [ ] **Step 1: Criar o projeto de testes e os testes que falham**

`GraveyardKeeper/StatusDosOrgaos.Tests/StatusDosOrgaos.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>disable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <!-- Só a lógica pura; nada de Assembly-CSharp aqui -->
    <Compile Include="..\StatusDosOrgaos\Core\*.cs" Link="Core\%(Filename)%(Extension)" />
  </ItemGroup>
</Project>
```

`GraveyardKeeper/StatusDosOrgaos.Tests/OrganClassifierTests.cs`:
```csharp
using StatusDosOrgaos.Core;
using Xunit;

namespace StatusDosOrgaos.Tests;

public class OrganClassifierTests
{
    private const string BodyPart = "BodyUniversalPart";

    [Theory]
    [InlineData("heart:heart_4_1")]
    [InlineData("brain:brain_-2_3")]
    [InlineData("intestine:intestine_0_0")]
    [InlineData("lungs:lungs_1")]
    [InlineData("heart_dark")]
    [InlineData("eye")]
    [InlineData("tongue")]
    [InlineData("scalp")]
    [InlineData("muscle")]
    public void Organs_AreAccepted(string id)
    {
        Assert.True(OrganClassifier.IsOrgan(id, BodyPart, OrganScope.Orgaos));
    }

    [Theory]
    [InlineData("flesh", BodyPart)]
    [InlineData("blood", BodyPart)]
    [InlineData("skull", BodyPart)]
    [InlineData("bone", BodyPart)]
    [InlineData("tr_bellas_ring", BodyPart)]
    [InlineData("heart:heart_4_1", "None")]
    [InlineData("axe", "Axe")]
    [InlineData(null, BodyPart)]
    [InlineData("", BodyPart)]
    public void NonOrgans_AreRejected(string id, string type)
    {
        Assert.False(OrganClassifier.IsOrgan(id, type, OrganScope.Orgaos));
    }

    [Fact]
    public void AllBodyPartsScope_AcceptsFleshButNotOtherTypes()
    {
        Assert.True(OrganClassifier.IsOrgan("flesh", BodyPart, OrganScope.TodasPartesDoCorpo));
        Assert.False(OrganClassifier.IsOrgan("axe", "Axe", OrganScope.TodasPartesDoCorpo));
    }
}
```

`GraveyardKeeper/StatusDosOrgaos.Tests/SkullFormatterTests.cs`:
```csharp
using StatusDosOrgaos.Core;
using Xunit;

namespace StatusDosOrgaos.Tests;

public class SkullFormatterTests
{
    [Fact]
    public void Icons_RedThenWhite_LikeTheGame()
    {
        Assert.Equal("+(rskull)(rskull)(rskull)(rskull), +(skull)", SkullFormatter.Icons(4, 1));
    }

    [Fact]
    public void Icons_OnlyRed()
    {
        Assert.Equal("+(rskull)(rskull)", SkullFormatter.Icons(2, 0));
    }

    [Fact]
    public void Icons_OnlyWhite()
    {
        Assert.Equal("+(skull)(skull)(skull)", SkullFormatter.Icons(0, 3));
    }

    [Fact]
    public void Icons_NegativeValuesUseMinusSign()
    {
        Assert.Equal("-(rskull)(rskull), +(skull)(skull)(skull)", SkullFormatter.Icons(-2, 3));
    }

    [Fact]
    public void Icons_ZeroZeroIsEmpty()
    {
        Assert.Equal("", SkullFormatter.Icons(0, 0));
    }

    [Fact]
    public void Summary_ShowsBothNumbers()
    {
        Assert.Equal("(rskull) Vermelhas: 4   (skull) Brancas: 1", SkullFormatter.Summary(4, 1));
    }

    [Fact]
    public void Summary_ShowsZeros()
    {
        Assert.Equal("(rskull) Vermelhas: 0   (skull) Brancas: 0", SkullFormatter.Summary(0, 0));
    }

    [Fact]
    public void Summary_ShowsNegatives()
    {
        Assert.Equal("(rskull) Vermelhas: -1   (skull) Brancas: 3", SkullFormatter.Summary(-1, 3));
    }
}
```

- [ ] **Step 2: Rodar e confirmar que falha**

Run: `dotnet test GraveyardKeeper/StatusDosOrgaos.Tests`
Expected: FAIL na compilação (`OrganClassifier` e `SkullFormatter` não existem).

- [ ] **Step 3: Implementar a lógica pura e o projeto do plugin**

`GraveyardKeeper/StatusDosOrgaos/Core/OrganClassifier.cs`:
```csharp
using System.Collections.Generic;

namespace StatusDosOrgaos.Core;

public enum OrganScope
{
    Orgaos,
    TodasPartesDoCorpo
}

public static class OrganClassifier
{
    private const string BodyPartType = "BodyUniversalPart";

    private static readonly HashSet<string> OrganBases = new HashSet<string>
    {
        "heart", "brain", "intestine", "lungs", "eye", "tongue", "scalp", "muscle"
    };

    public static bool IsOrgan(string id, string itemType, OrganScope scope)
    {
        if (string.IsNullOrEmpty(id) || itemType != BodyPartType) return false;
        if (scope == OrganScope.TodasPartesDoCorpo) return true;

        var baseId = id;
        var colon = baseId.IndexOf(':');
        if (colon >= 0) baseId = baseId.Substring(0, colon);
        if (baseId.EndsWith("_dark")) baseId = baseId.Substring(0, baseId.Length - "_dark".Length);

        return OrganBases.Contains(baseId);
    }
}
```

`GraveyardKeeper/StatusDosOrgaos/Core/SkullFormatter.cs`:
```csharp
using System;
using System.Text;

namespace StatusDosOrgaos.Core;

public static class SkullFormatter
{
    /// <summary>Reproduz o formato de Item.GetItemBodyModificators(): "+(rskull)…, +(skull)…".</summary>
    public static string Icons(int red, int white)
    {
        var text = "";
        if (red != 0) text = Append(text, red, "(rskull)");
        if (white != 0) text = Append(text, white, "(skull)");
        return text;
    }

    public static string Summary(int red, int white) =>
        $"(rskull) Vermelhas: {red}   (skull) Brancas: {white}";

    private static string Append(string text, int value, string icon)
    {
        var sb = new StringBuilder(text);
        if (sb.Length > 0) sb.Append(", ");
        sb.Append(value < 0 ? "-" : "+");
        for (var i = 0; i < Math.Abs(value); i++) sb.Append(icon);
        return sb.ToString();
    }
}
```

`GraveyardKeeper/StatusDosOrgaos/StatusDosOrgaos.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>disable</Nullable>
    <AssemblyName>StatusDosOrgaos</AssemblyName>
    <RootNamespace>StatusDosOrgaos</RootNamespace>
    <Version>1.0.0</Version>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <DebugType>portable</DebugType>
    <GamePath Condition="'$(GamePath)' == ''">D:\SteamLibrary\steamapps\common\Graveyard Keeper</GamePath>
    <Managed>$(GamePath)\Graveyard Keeper_Data\Managed</Managed>
    <OutputPath>$(GamePath)\BepInEx\plugins\$(AssemblyName)\</OutputPath>
  </PropertyGroup>

  <ItemDefinitionGroup>
    <Reference><Private>False</Private></Reference>
  </ItemDefinitionGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include="BepInEx"><HintPath>$(GamePath)\BepInEx\core\BepInEx.dll</HintPath></Reference>
    <Reference Include="0Harmony"><HintPath>$(GamePath)\BepInEx\core\0Harmony.dll</HintPath></Reference>
    <Reference Include="Assembly-CSharp"><HintPath>$(Managed)\Assembly-CSharp.dll</HintPath></Reference>
    <!-- NGUI (NGUIText.Alignment, usado nos parâmetros opcionais de BubbleWidgetTextData) -->
    <Reference Include="Assembly-CSharp-firstpass"><HintPath>$(Managed)\Assembly-CSharp-firstpass.dll</HintPath></Reference>
    <Reference Include="UnityEngine"><HintPath>$(Managed)\UnityEngine.dll</HintPath></Reference>
    <Reference Include="UnityEngine.CoreModule"><HintPath>$(Managed)\UnityEngine.CoreModule.dll</HintPath></Reference>
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Rodar os testes e confirmar que passam**

Run: `dotnet test GraveyardKeeper/StatusDosOrgaos.Tests`
Expected: PASS, 27 testes (9 + 9 + 1 do classificador, 8 do formatador).

- [ ] **Step 5: Commit**

```bash
git add GraveyardKeeper/.gitignore GraveyardKeeper/StatusDosOrgaos/StatusDosOrgaos.csproj GraveyardKeeper/StatusDosOrgaos/Core GraveyardKeeper/StatusDosOrgaos.Tests
git commit -m "feat(gyk-status-orgaos): classificador de órgãos e formatador de caveiras com testes"
```

---

### Task 2: Plugin, configuração e liberação dos ícones de caveira

**Files:**
- Create: `GraveyardKeeper/StatusDosOrgaos/Plugin.cs`
- Create: `GraveyardKeeper/StatusDosOrgaos/Patches/BodyModificatorsPatch.cs`

**Interfaces:**
- Consumes: `OrganClassifier.IsOrgan`, `OrganScope`, `SkullFormatter.Icons` (Task 1).
- Produces:
  - `Plugin.Log` (`ManualLogSource`)
  - as configs `Plugin.Enabled` (`ConfigEntry<bool>`), `Plugin.Scope` (`ConfigEntry<OrganScope>`), `Plugin.ShowIcons` (`ConfigEntry<bool>`), `Plugin.ShowSummary` (`ConfigEntry<bool>`)
  - `static bool Plugin.IsTargetOrgan(Item item)`

- [ ] **Step 1: Implementar o `Plugin`**

`GraveyardKeeper/StatusDosOrgaos/Plugin.cs`:
```csharp
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using StatusDosOrgaos.Core;

namespace StatusDosOrgaos;

[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin
{
    public const string Guid = "jonathan.gyk.statusdosorgaos";
    public const string Name = "Status dos Órgãos";
    public const string Version = "1.0.0";

    internal static ManualLogSource Log;
    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<OrganScope> Scope;
    internal static ConfigEntry<bool> ShowIcons;
    internal static ConfigEntry<bool> ShowSummary;

    private void Awake()
    {
        Log = Logger;

        Enabled = Config.Bind("1. Geral", "Ativado", true, "Mostra as caveiras dos órgãos no tooltip.");
        Scope = Config.Bind("1. Geral", "Escopo", OrganScope.Orgaos,
            "Orgaos = coração, cérebro, intestino, pulmões, olho, língua, couro cabeludo, músculo. " +
            "TodasPartesDoCorpo = inclui carne, sangue, gordura, pele, osso e caveira.");
        ShowIcons = Config.Bind("2. Exibição", "Mostrar ícones de caveira", true,
            "Libera a linha de ícones que o jogo normalmente esconde até mais tarde na história.");
        ShowSummary = Config.Bind("2. Exibição", "Mostrar linha com números", true,
            "Acrescenta 'Vermelhas: X  Brancas: Y' no tooltip.");

        Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, Guid);
        Log.LogInfo($"{Name} {Version} carregado.");
    }

    internal static bool IsTargetOrgan(Item item)
    {
        if (!Enabled.Value || item == null || item.IsEmpty()) return false;
        var def = item.definition;
        return def != null && OrganClassifier.IsOrgan(def.id, def.type.ToString(), Scope.Value);
    }
}
```

- [ ] **Step 2: Implementar o patch que tira a trava**

`GraveyardKeeper/StatusDosOrgaos/Patches/BodyModificatorsPatch.cs`:
```csharp
using HarmonyLib;
using StatusDosOrgaos.Core;

namespace StatusDosOrgaos.Patches;

/// <summary>
/// O jogo só mostra as caveiras quando show_q_hint (ex.: Ppar("p_cultist") > 0) é verdadeiro.
/// Para órgãos, devolvemos sempre o texto no mesmo formato, usando os valores reais do item
/// (incluindo modificadores do Organ Enhancer).
/// </summary>
[HarmonyPatch(typeof(Item), nameof(Item.GetItemBodyModificators))]
internal static class BodyModificatorsPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Item __instance, ref string __result)
    {
        if (!Plugin.ShowIcons.Value || !Plugin.IsTargetOrgan(__instance)) return true;

        __result = SkullFormatter.Icons(__instance.GetRedSkullsValue(), __instance.GetWhiteSkullsValue());
        return false;
    }
}
```

- [ ] **Step 3: Compilar**

Feche o jogo antes. Run: `dotnet build GraveyardKeeper/StatusDosOrgaos -c Release`
Expected: `0 Erro(s)` e `StatusDosOrgaos.dll` em `D:\SteamLibrary\steamapps\common\Graveyard Keeper\BepInEx\plugins\StatusDosOrgaos\`.

- [ ] **Step 4: Verificar no jogo**

1. Abra o jogo pela Steam e carregue um save. No `BepInEx\LogOutput.log`, procure `Status dos Órgãos 1.0.0 carregado.`
2. Passe o mouse sobre um coração no inventário (ou no baú). Expected: abaixo da descrição aparece um separador e os ícones (ex.: `+🔴🔴🔴🔴, +💀` para `heart_4_1`).
3. Passe o mouse sobre carne. Expected: com `Escopo = Orgaos`, o tooltip fica igual ao original.
4. Em um save onde os ícones já apareciam antes do mod: confira que eles aparecem **uma vez só**.
5. No F1, desligue "Mostrar ícones de caveira" e passe o mouse de novo. Expected: volta o comportamento original.

- [ ] **Step 5: Commit**

```bash
git add GraveyardKeeper/StatusDosOrgaos/Plugin.cs GraveyardKeeper/StatusDosOrgaos/Patches/BodyModificatorsPatch.cs
git commit -m "feat(gyk-status-orgaos): mostra ícones de caveira dos órgãos desde o início"
```

---

### Task 3: Linha numérica no tooltip

**Files:**
- Create: `GraveyardKeeper/StatusDosOrgaos/Patches/TooltipSummaryPatch.cs`

**Interfaces:**
- Consumes: `Plugin.IsTargetOrgan`, `Plugin.ShowSummary` (Task 2); `SkullFormatter.Summary` (Task 1).
- Produces: nada consumido por outras tarefas.

- [ ] **Step 1: Implementar o postfix**

`GraveyardKeeper/StatusDosOrgaos/Patches/TooltipSummaryPatch.cs`:
```csharp
using System.Collections.Generic;
using HarmonyLib;
using StatusDosOrgaos.Core;

namespace StatusDosOrgaos.Patches;

[HarmonyPatch(typeof(ItemDefinition), nameof(ItemDefinition.GetTooltipData))]
internal static class TooltipSummaryPatch
{
    [HarmonyPostfix]
    private static void Postfix(Item item, bool full_detail, List<BubbleWidgetData> __result)
    {
        if (!full_detail || __result == null) return;
        if (!Plugin.ShowSummary.Value || !Plugin.IsTargetOrgan(item)) return;

        var summary = SkullFormatter.Summary(item.GetRedSkullsValue(), item.GetWhiteSkullsValue());
        __result.Add(new BubbleWidgetTextData(summary, UITextStyles.TextStyle.TinyDescription));
    }
}
```

Se o compilador reclamar de ambiguidade em `nameof(ItemDefinition.GetTooltipData)`, porque existe também `GetTooltipDataCraftAt`, troque o atributo por `[HarmonyPatch(typeof(ItemDefinition), "GetTooltipData", new[] { typeof(Item), typeof(bool) })]`.

- [ ] **Step 2: Compilar**

Run: `dotnet build GraveyardKeeper/StatusDosOrgaos -c Release`
Expected: `0 Erro(s)`.

- [ ] **Step 3: Verificar no jogo**

1. Passe o mouse sobre `heart:heart_4_1`. Expected: última linha `(rskull) Vermelhas: 4   (skull) Brancas: 1`, com os ícones renderizados.
2. Passe o mouse sobre `heart:heart_0_0`. Expected: nenhum ícone na linha do jogo e a linha `Vermelhas: 0 … Brancas: 0`.
3. Melhore um órgão no Organ Enhancer (+1 branca) e passe o mouse sobre ele. Expected: o número de brancas sobe em 1.
4. Abra a tela de tecnologias (`TechUnlock`) e passe o mouse sobre um item que a tecnologia libera. Expected: sem a linha extra.
5. Passe o mouse sobre itens que não são órgãos (ferramenta, oração, comida). Expected: tooltip idêntico ao original.
6. Confira se o log não tem exceções de `TooltipSummaryPatch` ou `BodyModificatorsPatch`.

- [ ] **Step 4: Commit**

```bash
git add GraveyardKeeper/StatusDosOrgaos/Patches/TooltipSummaryPatch.cs
git commit -m "feat(gyk-status-orgaos): linha com número de caveiras no tooltip"
```

---

### Task 4: Documentação e pacote de release

**Files:**
- Create: `GraveyardKeeper/StatusDosOrgaos/README.md`
- Create: `GraveyardKeeper/StatusDosOrgaos/CHANGELOG.md`
- Modify: `GraveyardKeeper/.gitignore` (acrescentar `release/`)

**Interfaces:**
- Consumes: a DLL gerada nas Tasks 2–3.
- Produces: `GraveyardKeeper/StatusDosOrgaos/release/StatusDosOrgaos-1.0.0.zip`, com a estrutura `BepInEx/plugins/StatusDosOrgaos/StatusDosOrgaos.dll`.

- [ ] **Step 1: Escrever o README e o CHANGELOG**

`GraveyardKeeper/StatusDosOrgaos/README.md`:
```markdown
# Status dos Órgãos (Graveyard Keeper)

Mostra no tooltip de cada órgão quantas caveiras **vermelhas** e **brancas** ele dá, desde o início do jogo.
Normalmente o jogo só revela isso mais tarde na história. Não altera empilhamento nem nenhum outro item.

## Requisitos
- Graveyard Keeper (Steam 1.407 testado)
- BepInEx 5.4.x ([Graveyard Keeper BepInEx 5 Pack](https://www.nexusmods.com/graveyardkeeper/mods/79))

## Instalação
Extraia o zip na pasta do jogo (cria `BepInEx/plugins/StatusDosOrgaos/StatusDosOrgaos.dll`).

## Configuração (F1 no jogo ou `BepInEx/config/jonathan.gyk.statusdosorgaos.cfg`)
| Opção | Padrão | Efeito |
|---|---|---|
| Ativado | true | Liga/desliga |
| Escopo | Orgaos | `Orgaos` ou `TodasPartesDoCorpo` (inclui carne, sangue, gordura, pele, osso, caveira) |
| Mostrar ícones de caveira | true | Libera a linha de ícones do próprio jogo |
| Mostrar linha com números | true | Acrescenta "Vermelhas: X  Brancas: Y" |

## Desinstalação
Apague `BepInEx/plugins/StatusDosOrgaos`. O mod não grava nada no save.
```

`GraveyardKeeper/StatusDosOrgaos/CHANGELOG.md`:
```markdown
## 1.0.0
- Mostra as caveiras vermelhas/brancas de órgãos no tooltip desde o início do jogo.
- Linha numérica opcional; escopo configurável (órgãos ou todas as partes do corpo).
```

- [ ] **Step 2: Gerar o zip de release**

Run (PowerShell, na raiz do repositório):
```powershell
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$dir = "GraveyardKeeper\StatusDosOrgaos\release"
New-Item -ItemType Directory -Force $dir | Out-Null
$zip = (Resolve-Path $dir).Path + "\StatusDosOrgaos-1.0.0.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
$archive = [IO.Compression.ZipFile]::Open($zip, 'Create')
[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, "D:\SteamLibrary\steamapps\common\Graveyard Keeper\BepInEx\plugins\StatusDosOrgaos\StatusDosOrgaos.dll", "BepInEx/plugins/StatusDosOrgaos/StatusDosOrgaos.dll") | Out-Null
$archive.Dispose()
```
Expected: o zip contém `BepInEx/plugins/StatusDosOrgaos/StatusDosOrgaos.dll`.

- [ ] **Step 3: Ignorar a pasta de release**

Acrescente a linha `release/` em `GraveyardKeeper/.gitignore`.

- [ ] **Step 4: Commit**

```bash
git add GraveyardKeeper/.gitignore GraveyardKeeper/StatusDosOrgaos/README.md GraveyardKeeper/StatusDosOrgaos/CHANGELOG.md
git commit -m "docs(gyk-status-orgaos): README, changelog e empacotamento"
```
