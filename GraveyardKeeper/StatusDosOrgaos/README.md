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
