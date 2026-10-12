# Greenhouse Direct Upgrades — patch notes

## O pedido

O mod [NewGreenhouseUpgrades](https://www.nexusmods.com/stardewvalley/mods/35846) (fork
feito em 2025 do antigo `GreenhouseUpgrades` do Cecidelus, já incompatível com a 1.6)
funciona, mas só oferece o upgrade da estufa na loja da Robin depois de: (1) desbloquear
a estufa, (2) já ter falado com o Mago pelo menos uma vez, (3) ter amizade suficiente
com o Mago (`Hearts` no `config.json`), e (4) esperar uma carta do Mago que só chega no
dia seguinte. O pedido era remover tudo isso e deixar só "ter a estufa desbloqueada",
do mesmo jeito que qualquer outro upgrade de construção na Robin (tipo Coop → Big Coop).

## Por que isso virou um patch binário e não um mod novo compilado

Não consegui compilar C# novo neste ambiente: não há SDK do .NET instalado, a rede
deste container só enxerga infraestrutura da Anthropic (pypi, npm, apt do Ubuntu e o
CDN do dotnet retornam 403 "host not in allowlist"), e esta sessão não tem acesso a
shell na sua máquina (só consigo listar/ler/escrever arquivos nela, não rodar `dotnet
build`). Você tem um SDK funcional no seu PC (dá pra ver pelo `ForjaLivre/bin` e
`obj` neste repositório), mas eu não tenho como acioná-lo daqui.

Em vez de te entregar um `.cs` que só serve depois de compilado manualmente por você,
escrevi do zero (só com a biblioteca padrão do Python, sem internet) um leitor de
metadados ECMA-335 + desmontador de IL (`clr.py` + `ildasm.py`, incluídos aqui) e usei
isso pra entender exatamente a lógica compilada do `NewGreenhouseUpgrades.dll` e
aplicar um patch binário cirúrgico nela — trocando só os bytes específicos que
implementam as regras de amizade/carta, sem alterar mais nada (tamanho do arquivo
idêntico, todos os outros 114 métodos bit-a-bit iguais).

## O que a engenharia reversa revelou

- `ModEntry.Entry` registra uma **GameStateQuery customizada** chamada
  `GreenHouseSprinklers.BuildCondition` (`StardewValley.GameStateQuery.Register(...)`).
  É essa query que o jogo avalia **ao vivo, toda vez que o menu da Robin abre** — o
  mesmo mecanismo que qualquer upgrade nativo (Coop, Barn, etc.) usa via o campo
  `BuildCondition` de `Data/Buildings`.
- A implementação, `CheckForUpgrade(query, context)`:
  - Exige que o número do tier pedido seja exatamente `GetUpgradeLevel(estufa) + 1`
    (ou seja, "é o próximo tier") — isso é o que já faz o tier 2/3 só aparecerem depois
    do tier anterior estar construído, igual upgrade nativo de construção.
  - **E ALÉM DISSO** exigia `Game1.player.mailReceived.Contains("Bpendragon.GreenhouseSprinklers.WizardN")`
    (ou a variante `"...WizardNb"`) — ou seja, "a carta do Mago pra esse tier já chegou".
- Essa carta só é enfileirada (`Game1.addMailForTomorrow`, que **sempre** leva um dia
  pra chegar) por `AddLetterIfNeeded`, chamado em `OnDayEnding` só depois de confirmar
  que a estufa está desbloqueada. `AddLetterIfNeeded` ainda exigia
  `Game1.player.friendshipData.ContainsKey("Wizard")` (ter falado com o Mago alguma vez)
  antes mesmo de olhar o `Hearts` do `config.json`.

## O patch (`apply_patch.py`)

Três edições, todas trocando bytes por bytes de igual tamanho (opcodes/NOPs), sem
mexer em nenhum offset/RVA do resto do assembly:

1. `AddLetterIfNeeded`: o teste "já falei com o Mago alguma vez" é forçado a sempre
   ser verdadeiro (`pop; nop; ldc.i4.0` no lugar de `ldc.i4.0; ceq`).
2. `CheckForUpgrade`: os dois `mailReceived.Contains(...)` que testavam
   `"...WizardN"` / `"...WizardNb"` agora testam `"ccPantry"` / `"jojaPantry"` — os
   mesmos dois mail flags que `OnDayEnding` já usa pra saber se a estufa foi
   desbloqueada (community center ou rota da Joja). Como essas strings já existem no
   heap do assembly, reaproveitei os tokens existentes em vez de inflar o arquivo.
3. `OnDayEnding`: a chamada pra `AddLetterIfNeeded` foi removida (virou NOP) — a carta
   do Mago não serve mais pra nada, então o mod não manda mais ela.

Resultado: a Robin mostra o upgrade da estufa **no mesmo instante** em que
`ccPantry`/`jojaPantry` estiver no `mailReceived` do jogador (ou, pro tier 2/3, assim
que o tier anterior estiver construído) — sem esperar um dia, sem carta, sem amizade
com o Mago. Exatamente como qualquer outro upgrade de construção.

O `config.json` (`Hearts: 0` em todas as dificuldades) continua no pacote só por
inércia — não faz mais diferença nenhuma, já que `CheckForUpgrade` nem olha mais pra
amizade.

## Verificação

`apply_patch.py`, ao final, reconstrói a tabela de metadados do arquivo gerado e
redesmonta os 117 métodos do assembly (`ildasm.decode`) pra garantir que nenhum deles
ficou malformado. Além disso, script à parte comparou byte a byte contra o `.dll`
original: só os offsets das três edições acima mudaram, todo o resto é idêntico.

## Onde as coisas estão

- `NewGreenhouseUpgrades.dll` (patchado) foi implantado direto na sua instalação real
  do mod (`~/Games/Amethyst/Stardew Valley/mods/NewGreenhouseUpgrades/...`, que é pra
  onde os symlinks da pasta `Mods` do jogo apontam). O `.dll` original (antes de
  qualquer patch) ficou salvo ao lado como `NewGreenhouseUpgrades.dll.original.bak`.
- Aqui no repositório: os scripts de decompilação/patch (`clr.py`, `ildasm.py`,
  `apply_patch.py`), o `.dll` patchado final, e os manifests/config/content pack tal
  como estão hoje no seu jogo — pra referência e caso queira reaplicar isso numa
  atualização futura do mod original.

## Limitações que valem mencionar

- Não toquei em `BuildingToUpgrade`/nos tipos das construções (continuam todos
  `"Greenhouse"` internamente, restaurados depois da Robin construir cada tier) — é
  assim que os dois mods de estufa (o antigo e o novo) evitam quebrar a lógica
  hardcoded do jogo pra estufa (colheita 24/7, flags da community center, etc.). Não
  usei a cadeia nativa `BuildingToUpgrade` com tipos diferentes por tier porque isso
  exigiria validar mais a fundo se o motor do jogo (`Stardew Valley.dll`) trata esses
  casos com segurança — pelo que investiguei (`IsGreenhouse` é uma propriedade do
  *mapa*, não do tipo de construção), provavelmente seria seguro, mas a abordagem que
  os dois mods da comunidade já usam e validaram em jogo de verdade é essa que
  mantive.
- Se você trocar de save pra um em que a estufa **não** esteja desbloqueada ainda, o
  comportamento volta a ser "sem prerequisito nenhum de amizade" mesmo assim — o único
  gate que sobrou é `ccPantry`/`jojaPantry`, igual você pediu.

## Instalação no PC Windows (sessão seguinte, sem memória da anterior)

Numa sessão nova (sem acesso ao histórico da conversa que gerou o patch acima — só
achei este README ao listar a pasta), você pediu pra instalar esse mesmo mod da
estufa aqui no Windows (`D:\SteamLibrary\steamapps\common\Stardew Valley`), onde os
mods são gerenciados pelo Vortex.

### O que foi feito

1. Copiei `NewGreenhouseUpgrades.dll` (o já patchado, deste repositório),
   `manifest.json` e `config.json` direto pra
   `Mods\NewGreenhouseUpgrades\` (arquivo simples, sem passar pelo Vortex).
2. Nesse meio tempo o Vortex também baixou o mod **original** (não patchado) do
   Nexus (pasta de staging dele:
   `C:\Users\jfajo\AppData\Roaming\Vortex\stardewvalley\mods\NewGreenhouseUpgrades-35846-1-0-0-1752913571`)
   e deployou os links dele — só que a estrutura do próprio arquivo do mod já vem com
   uma pasta redundante (`NewGreenhouseUpgrades/NewGreenhouseUpgrades/...` e
   `NewGreenhouseUpgrades/[CP]NewGreenhouseUpgrades/...`), e o Vortex não desfaz esse
   aninhamento. Como isso caiu dentro da mesma pasta `Mods\NewGreenhouseUpgrades\` que
   eu já tinha criado (que já tem `manifest.json` direto nela), o SMAPI não desce mais
   um nível pra achar as duas subpastas do Vortex — ele já considera aquela pasta como
   "um mod resolvido" e para de procurar. Ou seja, mesmo sem a minha pasta manual, essa
   estrutura de duas subpastas dentro de uma só provavelmente já não seria lida pelo
   SMAPI (a heurística dele só desembrulha automaticamente quando existe **exatamente
   uma** subpasta, e aqui existem duas).
3. Confirmei por hash (`md5sum`) que o `.dll` que o Vortex baixou é **diferente** do
   `.dll` patchado deste repositório — o do Vortex ainda tem a checagem de amizade/carta
   original. Então a cópia manual no passo 1 é a que realmente importa; a cópia do
   Vortex, presa dentro da subpasta aninhada, fica órfã e inofensiva (SMAPI ignora).
4. O pacote de Content Patcher do mod (que finalmente tinha os mapas
   `Greenhouse1.tmx`/`Greenhouse2.tmx` que faltavam na entrega anterior — esses mapas
   nunca foram salvos neste repositório, só as configs) estava enterrado em
   `Mods\NewGreenhouseUpgrades\[CP]NewGreenhouseUpgrades\`. Copiei `manifest.json`,
   `content.json` e os dois `.tmx` de lá pra `Mods\[CP]NewGreenhouseUpgrades\` — nível
   certo, direto dentro de `Mods`, do lado do mod principal, igual aos outros pacotes
   `[CP] ...` que você já tinha instalado.
5. Percebi que o **Content Patcher** (o framework do Pathoschild, `Pathoschild.
   ContentPatcher`) não estava instalado em lugar nenhum de `Mods` — o que bloqueava
   não só o pacote visual da estufa, mas todos os outros mods `[CP] ...` da sua lista
   (Clint Reforged, Community Center, Stardew Valley Expanded, Ridgeside Village etc.).
   Avisei você, e você instalou pelo Vortex (Content Patcher 2.9.1, Nexus #1915).
   Confirmei o `manifest.json` deployado (`UniqueID: "Pathoschild.ContentPatcher"`,
   versão 2.9.1) antes de considerar o trabalho concluído.

### Estado final em `Mods\` (Windows)

```
Mods\NewGreenhouseUpgrades\manifest.json       ← flat, patchado (o que o SMAPI usa)
Mods\NewGreenhouseUpgrades\config.json
Mods\NewGreenhouseUpgrades\NewGreenhouseUpgrades.dll
Mods\NewGreenhouseUpgrades\NewGreenhouseUpgrades\...   ← lixo órfão do Vortex, ignorado pelo SMAPI
Mods\NewGreenhouseUpgrades\[CP]NewGreenhouseUpgrades\...  ← idem, ignorado
Mods\[CP]NewGreenhouseUpgrades\manifest.json   ← pacote visual, nível certo
Mods\[CP]NewGreenhouseUpgrades\content.json
Mods\[CP]NewGreenhouseUpgrades\assets\Greenhouse1.tmx
Mods\[CP]NewGreenhouseUpgrades\assets\Greenhouse2.tmx
Mods\ContentPatcher\...                        ← instalado depois, via Vortex
```

As duas subpastas órfãs dentro de `Mods\NewGreenhouseUpgrades\` não fazem mal (o
SMAPI simplesmente não chega nelas), mas ficaram lá porque esta sessão não tem
permissão de apagar arquivos na sua máquina — só de ler/escrever. Se quiser remover,
é mais simples fazer isso pelo próprio Vortex (purge/remover o mod e reinstalar
"limpo", já que a versão que importa está na cópia manual, não na dele).
