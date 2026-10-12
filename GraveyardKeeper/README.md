# Graveyard Keeper — Modding

Guia de referência para criar mods de código para o **Graveyard Keeper** (Lazy Bear Games / tinyBuild, Steam AppID `599140`).

Ao contrário do Stardew Valley (que usa SMAPI), o Graveyard Keeper é um jogo **Unity (Mono)**. Por isso os mods são **plugins BepInEx** escritos em C# que alteram o código do jogo em tempo de execução com **Harmony**. Não existe API oficial de modding.

---

## Ambiente desta máquina (verificado em 2026-10-12)

| Item | Valor |
|---|---|
| Instalação | `D:\SteamLibrary\steamapps\common\Graveyard Keeper` (Steam, 64-bit) |
| Build Steam | `22583570` (versão do jogo 1.407) |
| Unity | `2020.3.17` (runtime **Mono**, não IL2CPP; CLR 4.0) |
| BepInEx | `5.4.23.5` (já instalado; gerenciado pelo Vortex) |
| Harmony | `0Harmony.dll` 2.9.0 (vem com o BepInEx) |
| Plugins instalados | ConfigurationManager, AutoLootHeavies, Exhaustless, GKNoInflation, IncreaseInventory, NoCooldownTeleport, OyasumiStack, SuperStrongMod |
| .NET SDKs | 6.0.428 e 10.0.401 |

DLLs do jogo para referenciar: `Graveyard Keeper_Data\Managed\`
- `Assembly-CSharp.dll`: **todo o código do jogo** (a mais importante)
- `Assembly-CSharp-firstpass.dll`: plugins de terceiros (FlowCanvas/NodeCanvas etc.)
- `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.IMGUIModule.dll`, `UnityEngine.InputLegacyModule.dll`, `UnityEngine.UI.dll`, …
- Outras incluídas no jogo: `Newtonsoft.Json.dll`, `DOTween.dll`, `Rewired_Core.dll` (input), `Sirenix.*` (Odin).

DLLs do BepInEx: `BepInEx\core\` (`BepInEx.dll`, `0Harmony.dll`, …)

> **Steam e GOG são diferentes:** a Steam distribui a versão 1.407 em 64-bit, e a GOG distribui a 1.405 em 32-bit. O `Assembly-CSharp.dll` muda entre as duas, então um mod compilado contra uma pode quebrar na outra.

---

## 1. Como funciona o carregamento

```
Graveyard Keeper.exe
 └─ winhttp.dll (Unity Doorstop)  → lê doorstop_config.ini
     └─ BepInEx\core\BepInEx.Preloader.dll
         └─ Chainloader → carrega cada .dll em BepInEx\plugins\**
             └─ sua classe : BaseUnityPlugin  → Awake() → Harmony.PatchAll()
```

Estrutura de pastas relevante:

```
Graveyard Keeper/
├── BepInEx/
│   ├── core/          ← DLLs do BepInEx + Harmony
│   ├── plugins/       ← mods (.dll), de preferência um subdiretório por mod
│   ├── config/        ← arquivos .cfg (TOML) gerados pelos mods
│   ├── patchers/      ← preloader patchers (raro)
│   └── LogOutput.log  ← log de cada execução (primeiro lugar para olhar erros)
├── doorstop_config.ini
└── winhttp.dll
```

Histórico: os mods antigos usavam o **QModManager** (pasta `QMods/` + `mod.json`). Ele está **obsoleto**, e os mods de QMods não funcionam com BepInEx. Para mods novos, use só BepInEx 5.

---

## 2. Requisitos para desenvolver

1. **Graveyard Keeper** com **BepInEx 5** instalado (já está).
   - Instalação manual: [Graveyard Keeper BepInEx 5 Pack (Nexus #79)](https://www.nexusmods.com/graveyardkeeper/mods/79). Extraia na pasta do jogo e não instale pelo Vortex. O pack já vem com o Configuration Manager (abre com **F1** no jogo).
   - Pelo Vortex: [extensão de Graveyard Keeper para o Vortex](https://www.nexusmods.com/site/mods/571), com instalação do BepInEx em um clique (64-bit para Steam, 32-bit para GOG).
2. **.NET SDK 6+** (já instalado). O projeto compila para **.NET Framework** (`net481` ou `net472`), usando os *reference assemblies* via NuGet. Não é preciso instalar o .NET Framework Developer Pack.
3. **Editor**: VS Code com C# Dev Kit, Visual Studio 2022 ou Rider.
4. **Decompilador** (essencial: não existe documentação do código do jogo):
   - [dnSpyEx](https://github.com/dnSpyEx/dnSpy) (Windows; navegar, buscar e até depurar)
   - [ILSpy](https://github.com/icsharpcode/ILSpy) (multiplataforma; também existe extensão para VS Code)
   - Abra `Graveyard Keeper_Data\Managed\Assembly-CSharp.dll`.
5. Opcional: [UnityExplorer (BepInEx 5 Mono)](https://github.com/yukieiji/UnityExplorer), para inspecionar GameObjects, componentes e variáveis em tempo real dentro do jogo.

---

## 3. Criando um plugin

### Opção A: template oficial do BepInEx

```powershell
dotnet new install BepInEx.Templates::2.0.0-be.4 --nuget-source https://nuget.bepinex.dev/v3/index.json
dotnet new bepinex5plugin -n MeuModGYK -T net472 -U 2020.3.17
```

O template já configura o `BepInEx.Core`, o `BepInEx.PluginInfoProps` (gera a classe `MyPluginInfo`) e o `UnityEngine.Modules`. Depois disso, adicione a referência ao `Assembly-CSharp.dll` do jogo (veja abaixo).

### Opção B: `.csproj` manual (padrão usado pelos mods do p1xel8ted)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net481</TargetFramework>
    <LangVersion>latest</LangVersion>
    <AssemblyName>MeuModGYK</AssemblyName>
    <BepInExPluginGuid>jonathan.gyk.meumod</BepInExPluginGuid>
    <BepInExPluginName>Meu Mod GYK</BepInExPluginName>
    <Version>1.0.0</Version>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>

    <GamePath>D:\SteamLibrary\steamapps\common\Graveyard Keeper</GamePath>
    <Managed>$(GamePath)\Graveyard Keeper_Data\Managed</Managed>
    <!-- compila direto na pasta de plugins -->
    <OutputPath>$(GamePath)\BepInEx\plugins\$(AssemblyName)</OutputPath>
  </PropertyGroup>

  <ItemDefinitionGroup>
    <Reference><Private>False</Private></Reference>   <!-- não copiar DLLs do jogo -->
  </ItemDefinitionGroup>

  <ItemGroup>
    <PackageReference Include="BepInEx.PluginInfoProps" Version="2.1.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include="BepInEx"><HintPath>$(GamePath)\BepInEx\core\BepInEx.dll</HintPath></Reference>
    <Reference Include="0Harmony"><HintPath>$(GamePath)\BepInEx\core\0Harmony.dll</HintPath></Reference>
    <Reference Include="Assembly-CSharp"><HintPath>$(Managed)\Assembly-CSharp.dll</HintPath></Reference>
    <Reference Include="Assembly-CSharp-firstpass"><HintPath>$(Managed)\Assembly-CSharp-firstpass.dll</HintPath></Reference>
    <Reference Include="UnityEngine"><HintPath>$(Managed)\UnityEngine.dll</HintPath></Reference>
    <Reference Include="UnityEngine.CoreModule"><HintPath>$(Managed)\UnityEngine.CoreModule.dll</HintPath></Reference>
    <Reference Include="UnityEngine.IMGUIModule"><HintPath>$(Managed)\UnityEngine.IMGUIModule.dll</HintPath></Reference>
    <Reference Include="UnityEngine.InputLegacyModule"><HintPath>$(Managed)\UnityEngine.InputLegacyModule.dll</HintPath></Reference>
  </ItemGroup>
</Project>
```

Pode ser necessário adicionar `https://nuget.bepinex.dev/v3/index.json` como fonte NuGet (em um `nuget.config`) para o pacote `BepInEx.PluginInfoProps`. Outra opção é não usar o pacote e escrever as constantes de GUID, nome e versão à mão.

### `Plugin.cs` mínimo

```csharp
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace MeuModGYK;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    internal static ConfigEntry<float> Multiplicador;

    private void Awake()
    {
        Log = Logger;
        // Gera BepInEx/config/<GUID>.cfg; editável in-game pelo Configuration Manager (F1)
        Multiplicador = Config.Bind("Geral", "Multiplicador", 2f, "Fator aplicado ao ...");

        Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, MyPluginInfo.PLUGIN_GUID);
        Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} carregado!");
    }
}
```

### Patches Harmony

```csharp
using HarmonyLib;

namespace MeuModGYK;

[HarmonyPatch]
public static class Patches
{
    // Prefix: roda ANTES do método original. Retornar false pula o original.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SmartWeatherState), nameof(SmartWeatherState.Update))]
    public static void SemNeblina(SmartWeatherState __instance)
    {
        if (!MainGame.game_started || !__instance) return;
        if (__instance.type == SmartWeatherState.WeatherType.Fog)
            __instance.value = 0f;
    }

    // Postfix: roda DEPOIS; pode alterar o retorno via ref __result.
    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(AlgumaClasse), nameof(AlgumaClasse.Metodo))]
    // public static void Depois(ref int __result) => __result *= 2;
}
```

Parâmetros especiais do Harmony: `__instance` (o objeto), `__result` (o retorno), `___campoPrivado` (três underscores, acesso a um campo), `__state` (passa dados do prefix para o postfix). Para trocar instruções IL no meio de um método, use um `[HarmonyTranspiler]`. O mod *Longer Days* faz isso, trocando `Time.deltaTime` dentro de `EnvironmentEngine.Update`. Para acessar membros privados, use `AccessTools.Field/Method` ou `Traverse`.

Documentação: [Harmony](https://harmony.pardeike.net/articles/intro.html) · [BepInEx](https://docs.bepinex.dev/).

> **Membros privados:** se você referenciar o `Assembly-CSharp` diretamente, o compilador não deixa acessar membros `private` (como `_enabled`). Uma opção é usar o [BepInEx.AssemblyPublicizer.MSBuild](https://github.com/BepInEx/BepInEx.AssemblyPublicizer) (`<Reference ... Publicize="true" />`), que deixa tudo público só na compilação. Os mods do p1xel8ted fazem isso, e por isso suprimem o aviso `Publicizer001`.

---

## 4. Classes do jogo úteis (ponto de partida)

Achados no código de mods existentes. Confirme sempre no dnSpy/ILSpy, porque os nomes do jogo seguem `snake_case`.

| Classe / membro | Para que serve |
|---|---|
| `MainGame.me`, `MainGame.game_started` | Singleton principal; checar se há save carregado |
| `MainGame.me.player` | `WorldGameObject` do jogador |
| `MainGame.me.save` | Dados do save atual |
| `WorldGameObject` | Qualquer objeto do mundo (NPCs, mesas, baús, túmulos) |
| `Item`, `ItemDefinition` | Itens e suas definições |
| `GameBalance.me` | Banco de dados do jogo (itens, crafts, techs, objetos), carregado de recursos |
| `CraftDefinition`, `CraftComponent` | Receitas e fila de crafting |
| `EnvironmentEngine` | Tempo, dia e noite |
| `SmartWeatherState` | Clima (neblina, vento, chuva) |
| `CustomFlowScript`, `FlowGraph` (FlowCanvas) | Scripts de diálogo e cutscenes |
| `GUIElements.me` | Interface do jogo |

Para entender os dados do jogo, existem os mods/ferramentas **GameBalanceDumper** e **FlowScriptDumper** no repositório do p1xel8ted. Eles exportam para JSON todas as definições de itens, crafts e scripts.

---

## 5. Fluxo de trabalho

1. Decompilar `Assembly-CSharp.dll` e achar a classe e o método que controlam o comportamento desejado.
2. Criar o projeto (seção 3) dentro de `GraveyardKeeper/<NomeDoMod>/`.
3. Escrever os patches Harmony e as configurações (`Config.Bind`).
4. Rodar `dotnet build`. Com o `OutputPath` apontando para `BepInEx\plugins\<NomeDoMod>`, a DLL já vai para o lugar certo.
5. **Fechar o jogo antes de compilar**, porque a DLL fica travada enquanto ele está aberto.
6. Abrir o jogo pela Steam e conferir `BepInEx\LogOutput.log`.
7. Release: zipar a pasta `BepInEx\plugins\<NomeDoMod>\` mantendo a estrutura `BepInEx/plugins/...`, para funcionar com Vortex e com instalação manual. Publicar no [Nexus Mods](https://www.nexusmods.com/graveyardkeeper).

### Depuração

- **Console ao vivo**: em `BepInEx\config\BepInEx.cfg`, seção `[Logging.Console]`, coloque `Enabled = true`.
- **Log**: `BepInEx\LogOutput.log`. Exceções de patch aparecem aí com stack trace.
- **Depurador**: o `doorstop_config.ini` tem `debug_enabled = true` e `debug_address = 127.0.0.1:10000`. Com isso dá para anexar o dnSpyEx ou o Rider ao processo Mono. Para breakpoints funcionarem, compile com `<DebugType>portable</DebugType>`.
- **Vortex**: a pasta `BepInEx` desta instalação é gerenciada pelo Vortex (existe o arquivo `__folder_managed_by_vortex`). Colocar a DLL direto em `plugins\` funciona, mas um *deploy* do Vortex pode avisar que há arquivos externos. Isso não causa problema.

### Linux / Steam Deck (Proton)

Use a versão **Windows** do BepInEx e adicione nas opções de inicialização da Steam:
```
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

---

## 6. Boas práticas

- Use um GUID único no formato `autor.gyk.nomedomod`.
- Use `Path.Combine(Paths.ConfigPath, ...)` e `Paths.PluginPath` do BepInEx em vez de caminhos fixos.
- Prefira **Prefix e Postfix** a Transpiler. Quando usar Transpiler, avise no log se nenhum trecho do IL foi encontrado, porque um transpiler que não casa com nada falha em silêncio.
- Proteja os patches com `if (!MainGame.game_started) return;`. Muitos métodos também rodam no menu principal.
- Guarde o valor de `ConfigEntry` em cache (via `SettingChanged`) quando ele for lido em `Update()` a cada frame.
- Não salve dados próprios dentro do save do jogo sem necessidade. Mods que mudam a estrutura do save podem corromper partidas quando forem removidos.
- Teste com e sem as DLCs (*Stranger Sins*, *Game of Crone*, *Better Save Soul*), porque algumas classes e objetos só existem com elas.
- Atualizações do jogo podem quebrar os patches. Recompile contra a nova `Assembly-CSharp.dll` e confira o log.

---

## 7. Referências

**Ferramentas e bibliotecas**
- [BepInEx — documentação](https://docs.bepinex.dev/) · [tutorial de plugin](https://docs.bepinex.dev/articles/dev_guide/plugin_tutorial/1_setup.html) · [GitHub](https://github.com/BepInEx/BepInEx)
- [Harmony — documentação](https://harmony.pardeike.net/articles/intro.html)
- [BepInEx.AssemblyPublicizer](https://github.com/BepInEx/BepInEx.AssemblyPublicizer)
- [dnSpyEx](https://github.com/dnSpyEx/dnSpy) · [ILSpy](https://github.com/icsharpcode/ILSpy) · [UnityExplorer](https://github.com/yukieiji/UnityExplorer)

**Graveyard Keeper**
- [Nexus Mods — Graveyard Keeper](https://www.nexusmods.com/graveyardkeeper)
- [Graveyard Keeper BepInEx 5 Pack (Nexus #79)](https://www.nexusmods.com/graveyardkeeper/mods/79)
- [Extensão do Vortex para GYK (Nexus site #571)](https://www.nexusmods.com/site/mods/571)
- [p1xel8ted/Graveyard-Keeper-Mods](https://github.com/p1xel8ted/Graveyard-Keeper-Mods): cerca de 38 mods BepInEx open source (GPL-3.0). **É o melhor material de estudo**: veja `src/Directory.Build.props` para a configuração de build e `src/<Mod>/Patches.cs` para exemplos de patches.
- [mdn522/GYK-TeleportHotkey](https://github.com/mdn522/GYK-TeleportHotkey): exemplo antigo (era QModManager) com a lista de DLLs referenciadas.
- [Discussão na Steam sobre mods quebrados, BepInEx e GYK Helper](https://steamcommunity.com/app/599140/discussions/0/3972799233030828289)
- Discord do p1xel8ted (mods de GYK): https://discord.gg/Dy5ApMYYY8
