# Stardew Valley Modding

Este repositório é dedicado à criação de mods para o Stardew Valley usando **SMAPI**. Este documento resume o essencial do [Guia do Modder — Começando](https://pt.stardewvalleywiki.com/Modifica%C3%A7%C3%B5es:Guia_do_Modder/Come%C3%A7ando) da Stardew Valley Wiki, para orientar o trabalho neste projeto.

## Visão geral

- Um **mod SMAPI** usa a API do [SMAPI](https://smapi.io/) para estender a lógica do jogo (responder a eventos, rodar código periodicamente, alterar assets/dados, etc).
- Mods são escritos em **C#** rodando sobre .NET, já que o próprio Stardew Valley usa XNA/MonoGame.
- Também é possível criar mods **sem programar**, via *content packs* (ex: [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915)), que usam apenas JSON, imagens etc.
- O SMAPI cuida de: carregar o mod, expor APIs/eventos, reescrever o mod para compatibilidade multiplataforma (Linux/Mac/Windows), corrigir quebras após updates do jogo, interceptar erros/crashes, checar atualizações e compatibilidade.

## Requisitos do ambiente

1. **Stardew Valley** instalado.
2. **[SMAPI](https://smapi.io/)** instalado.
3. Um **IDE**:
   - Linux: [MonoDevelop](http://www.monodevelop.com/) (ou Visual Studio Code com extensões C#, na prática o mais usado hoje).
   - Windows: Visual Studio Community (ativar workload *.NET Desktop Development*).
   - Mac: Visual Studio para Mac.
4. **.NET 5 SDK** (ou a versão exigida pela versão atual do SMAPI/jogo — confira o [dotnet download](https://dotnet.microsoft.com/en-us/download/dotnet/5.0)).
5. Pacote NuGet **[Pathoschild.Stardew.ModBuildConfig](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig)** — referência automática do jogo/SMAPI e build multiplataforma.

Como o ambiente é Linux, o fluxo recomendado é: `dotnet` CLI + editor (VS Code) em vez de Visual Studio completo.

## Estrutura de um mod básico

Projeto = biblioteca de classes (.dll), **não** "Class Library (.NET Framework)" — usar .NET 5+.

```
MeuMod/
├── MeuMod.csproj
├── ModEntry.cs
└── manifest.json
```

### `ModEntry.cs` (ponto de entrada)

```csharp
using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace MeuMod
{
    /// <summary>O ponto de entrada do mod.</summary>
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            this.Monitor.Log($"{Game1.player.Name} apertou o botão {e.Button}.");
        }
    }
}
```

- A classe principal deve herdar de `Mod` (SMAPI detecta subclasses automaticamente).
- `Entry(IModHelper helper)` é chamado pelo SMAPI ao carregar o mod.
- `helper` dá acesso conveniente às APIs do SMAPI (eventos, config, tradução, reflexão, etc).

### `manifest.json`

Obrigatório em todo mod — descreve o mod para o SMAPI:

```json
{
    "Name": "<nome do mod>",
    "Author": "<seu nome>",
    "Version": "1.0.0",
    "Description": "<uma ou duas frases sobre o mod>",
    "UniqueID": "<seu nome>.<nome do mod>",
    "EntryDll": "<nome do projeto>.dll",
    "MinimumApiVersion": "2.9",
    "UpdateKeys": []
}
```

## Fluxo de trabalho

1. Criar o projeto de biblioteca de classes (.NET 5+).
2. Referenciar `Pathoschild.Stardew.ModBuildConfig` (via NuGet).
   - Se `StardewModdingAPI` não for encontrado, definir a propriedade `GamePath` no `.csproj` apontando para o diretório do jogo.
3. Escrever o código em `ModEntry.cs` usando eventos/APIs do SMAPI.
4. Criar o `manifest.json`.
5. Compilar (`dotnet build`) — o pacote de build copia automaticamente o mod para a pasta `Mods` do jogo.
6. Rodar o jogo através do SMAPI para testar.
7. Empacotar em `.zip` para lançamento (release).

## Boas práticas multiplataforma

O SMAPI ajusta o mod automaticamente para Linux/Mac/Windows, mas:

- Usar `Path.Combine(...)` para montar caminhos de arquivo — nunca hardcodar `\` ou `/`.
  ```csharp
  // ✘ Evitar
  string path = Helper.DirectoryPath + "\\assets\\asset.xnb";

  // ✓ Correto
  string path = Path.Combine(Helper.DirectoryPath, "assets", "asset.xnb");
  ```
- Usar `Helper.DirectoryPath` em vez de tentar descobrir o caminho do mod manualmente (`Assembly.GetCallingAssembly().Location` pode quebrar quando o SMAPI reescreve o assembly).

## Depuração / troubleshooting

- Verificar mensagens de erro na build (`dotnet build`, ou painel de erros do IDE).
- Consultar o [Guia de Teste e Solução de Problemas](https://pt.stardewvalleywiki.com/Modifica%C3%A7%C3%B5es:Guia_do_Modder/Teste_e_Solu%C3%A7%C3%A3o_de_Problemas) da wiki.
- Comunidade: canal `#modding` no [Stardew Valley Discord](https://discord.gg/stardewvalley).

## Indo além

- **APIs do SMAPI**: eventos, config, content (load & edit), data, input, logging, reflection, multiplayer, translation, update checks, utilities, content packs, console commands, integrações, [Harmony patching](https://harmony.pardeike.net/) (para modificar código do jogo diretamente).
- **Decompilar o código do jogo** para entender seu funcionamento interno:
  - Windows: [dotPeek](https://www.jetbrains.com/decompiler/) → botão direito em Stardew Valley → *Export to Project*.
  - Linux/Mac: abrir `StardewValley.exe` no MonoDevelop (`File > Open`), mudar *Language* do Summary para C#.
  - Para arquivos XNB (dados/imagens), ver documentação sobre edição de XNB.
- **Content Patcher**: alternativa sem código para editar imagens/dados do jogo via JSON — considerar quando o objetivo é apenas retexturizar/ajustar dados em vez de nova lógica.

## Referências

- [SMAPI](https://smapi.io/)
- [Guia do Modder — Começando](https://pt.stardewvalleywiki.com/Modifica%C3%A7%C3%B5es:Guia_do_Modder/Come%C3%A7ando)
- [Índice de Modificações da Wiki](https://pt.stardewvalleywiki.com/Modifica%C3%A7%C3%B5es:%C3%8Dndice)
- [Pathoschild.Stardew.ModBuildConfig](https://github.com/Pathoschild/Stardew.ModBuildConfig)
- [Content Patcher (Nexus Mods)](https://www.nexusmods.com/stardewvalley/mods/1915)
