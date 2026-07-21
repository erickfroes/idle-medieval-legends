# TASK 003 — Catálogo de conteúdo orientado a dados

Data: 2026-07-21

Branch: `feat/data-driven-content-catalog`

Unity: `6000.5.4f1 (d550df8bd089)`

## Objetivo

Criar uma fundação local, orientada a dados e somente leitura para definições
estáticas de heróis, itens, equipamentos, materiais, receitas, profissões,
raridades e Tiers. O catálogo auxilia apresentação e previsão no cliente, mas
não contém nem concede inventário, moeda, progresso, resultado de crafting,
gacha ou qualquer outra propriedade de jogador.

## Arquitetura criada

`Domain/Content` contém os tipos puros de catálogo, o agregado
`ContentCatalog`, o índice `ContentCatalogLookup` e o
`ContentCatalogValidator`. As definições recebem todos os valores pelo
construtor, copiam coleções defensivamente e expõem somente propriedades e
coleções de leitura.

`ContentCatalogAsset`, no assembly Config, é a fronteira de autoria do Unity.
Ele serializa DTOs privados e converte esses dados para um novo snapshot Domain.
A única operação que substitui o conteúdo do asset é compilada sob
`UNITY_EDITOR`; ela não existe no player. Essa separação também deixa os DTOs
prontos para receber mapeamentos simples de JSON ou CSV no futuro.

`GameManager` recebe uma referência explícita ao asset pela composição do
Bootstrap, valida-o e constrói o lookup antes de entrar em `Ready`. Não há
`Resources.Load`, busca em cena ou singleton adicional para localizar o
catálogo. O catálogo em si não depende de uma cena.

O assembly `IdleMedievalLegends.Editor.ContentCatalog` contém o gerador do
exemplo e o menu **Tools > Idle Medieval Legends > Validate Content Catalog**.
As mensagens incluem severidade, tipo, ID, motivo e o asset como contexto do
Console. O resumo informa totais gerais e por Tier, raridade e profissão.

## Tipos de conteúdo

- `HeroDefinition`: identidade, texto, arquétipo, atributos-base, raridade,
  tags e IDs reservados para habilidades futuras;
- `ItemDefinition`: metadados comuns de itens, flags, empilhamento, ícone e tags;
- `EquipmentDefinition`: slot, orçamento, afixos permitidos, requisitos,
  profissão de origem, aprimoramento e binding;
- `MaterialDefinition`: categoria, refino, saída refinada, fontes e stack;
- `RecipeDefinition`: saída, requisitos profissionais/estação, duração, custos,
  ingredientes, catalisadores, elegibilidade Mítica e comissões;
- `ProfessionDefinition`: níveis, desbloqueios, graus, Tiers, maestrias e bônus;
- `RarityDefinition`: multiplicadores, afixos, aprimoramento, cor, ordenação e
  metadata econômica apenas informativa;
- `TierDefinition`: nível profissional, orçamento e multiplicadores padrão;
- `ContentCatalog` e `ContentCatalogLookup`: snapshot e índices runtime.

## Estratégia de IDs e enums persistidos

IDs são textos explícitos, imutáveis por convenção e independentes de nome
visual, arquivo, GUID Unity ou posição em lista. O validador aceita somente
letras minúsculas ASCII, números e underscore e verifica unicidade global.
Nenhum ID é gerado em runtime.

Foram adicionados os contratos pedidos com valores explícitos:

- `Rarity`: `Common=0` até `Mythic=5`;
- `ContentTier`: `Tier1=1` até `Tier9=9`;
- `ProfessionType`: `Blacksmith=1` até `Gatherer=5`;
- `ProfessionRank` existente foi preservado, pois já era compatível.

Os tipos anteriores `GameRarity`, `ItemTier` e `CraftingProfession` não foram
renomeados nem reordenados. `LegacyProgressionTypeAdapter` oferece conversões
explícitas e validadas nos dois sentidos. `ProfessionType` preserva 1..5 para
ser compatível com `CraftingProfession`; o sentinela legado `None=0` não é uma
profissão válida no catálogo.

Multiplicadores serializados pelo Unity usam basis points inteiros e são
convertidos para `decimal` no snapshot Domain. Custos de ouro e quantidades
inteiras usam `long`.

## Validações

O lookup só é construído após relatório sem erros. Entre as rejeições estão:

- definição ou ID nulo/vazio, formato inválido e ID global duplicado;
- Tier, raridade, profissão, arquétipo, item type, slot ou binding inválido;
- referência inexistente de saída, ingrediente, refino ou profissão;
- receita sem saída, quantidade positiva ou ingredientes, salvo quando marcada
  explicitamente como gratuita;
- duração, Foco ou ouro negativos e ingrediente/catalisador não positivo;
- stack inválido e item não empilhável com stack diferente de um;
- equipamento sem slot, orçamento, nível ou limite coerente com a raridade;
- receita Mítica fora de T9/Deus;
- thresholds duplicados, ausentes, fora do nível máximo ou não crescentes;
- metadados de raridade/Tier ausentes ou duplicados.
- ausência de qualquer uma das cinco profissões persistidas;
- `Equipment` ou `Material` inserido na coleção genérica de itens em vez da
  coleção especializada correspondente.

IDs futuros de habilidade e de afixo são apenas referências reservadas nesta
tarefa, pois seus catálogos ainda não existem; eles são verificados quanto a
texto vazio/duplicado, mas não resolvidos.

## Conteúdo de demonstração

O asset `Assets/_Game/Data/Content/ContentCatalog.asset` foi criado pela API do
Unity e contém:

- heróis: Paladino, Arqueira e Mago;
- materiais: Minério de Ferro T1, Lingote de Ferro T1, Couro Cru T1, Couro
  Tratado T1 e Essência Arcana T1;
- equipamentos: Espada de Ferro T1, Túnica de Couro T1 e Anel Arcano T1;
- receitas: refino do lingote e criação dos três equipamentos;
- seis definições de raridade, nove metadados de Tier e cinco profissões.

Somente o conteúdo funcional T1 necessário ao exemplo foi criado. Os metadados
dos nove Tiers existem para centralizar o eixo e validar thresholds, mas não há
heróis, materiais, equipamentos ou receitas demonstrativas T2–T9.

As cadeias seguem a responsabilidade do GDD: Ferreiro refina minério e produz a
espada; Costureiro produz a túnica com couro tratado; Encantador produz o anel
com essência. Dependências adicionais em T1 aparecem somente como catalisadores
opcionais, mantendo o onboarding de T1–T2 pouco acoplado.

## Testes

`ContentCatalogTests.cs` adiciona 18 casos de teste sem dependência de cena cobrindo:

- catálogo demonstrativo válido e resumo;
- ID vazio e ID duplicado;
- referência inexistente;
- receita sem saída/ingredientes;
- Tier e raridade inválidos;
- quantidade de ingrediente e stack inválidos;
- lookup existente, tipado e ID inexistente;
- equipamento sem slot;
- thresholds profissionais inconsistentes;
- profissão persistida ausente e tipo especializado na coleção genérica;
- bloqueio de mutação das coleções runtime;
- valores numéricos dos enums e adaptadores legados.

Os testes de bootstrap existentes foram atualizados para fornecer um catálogo
válido e confirmar que o `GameManager` materializa o lookup.

## Validação executada e resultados reais

Logs e XML ficaram em
`%TEMP%/IdleMedievalLegends-validation-task003`.

- importação/compilação final: código `0`, sem `error CS` ou `warning CS`;
- validação do catálogo: código `0`; 1 asset, 3 heróis, 8 itens, 3
  equipamentos, 5 materiais, 4 receitas, 0 erros e 0 avisos;
- validação do Bootstrap: código `0`, composição válida;
- validação estrutural: código `0`, projeto válido e um aviso preexistente de
  `DefaultCompany`;
- EditMode: XML `Passed`, 51 executados, 51 aprovados, 0 falhas, 0 ignorados e
  0 inconclusivos;
- PlayMode: XML `Passed`, 1 smoke test executado e aprovado, sem falhas,
  ignorados ou inconclusivos;
- segunda geração do Bootstrap: código `0`; hashes SHA-256 do catálogo, cena,
  dois assets de balanceamento e Build Settings permaneceram idênticos;
- auditoria estática: 9 `.asmdef` válidos, referências resolvidas, 98 GUIDs de
  `.meta` únicos, sem whitespace de fonte, marcadores pendentes ou paths gerados.

A primeira compilação encontrou uma referência direta ausente do assembly
Bootstrap para Domain (`CS0234`). O `.asmdef` foi corrigido e essa tentativa não
foi tratada como sucesso; todos os resultados acima são posteriores à correção.

## Decisões

- definições runtime imutáveis e autoria Unity foram separadas;
- IDs do catálogo são textuais; enums representam eixos fechados, não identidade;
- raridades e Tiers têm todas as definições de metadata, enquanto conteúdo
  demonstrativo permanece apenas em T1;
- multiplicadores de autoria usam basis points para serialização determinística;
- o Bootstrap cria o exemplo somente quando o asset não existe; não sobrescreve
  conteúdo já autorado. O menu “Generate or Reset” deixa o reset explícito;
- o cliente valida e apresenta o catálogo local, mas o backend futuro continua
  autoridade sobre operações econômicas e versões oficiais.

## Limitações e riscos

- não há versionamento/hash de catálogo nem negociação de versão com backend;
- não há importador JSON/CSV, somente a fronteira de DTOs preparada;
- não existem catálogos de habilidades ou afixos para resolver esses IDs;
- o asset é monolítico e adequado ao exemplo; conteúdo grande pode exigir
  divisão por módulos e build-time aggregation;
- alterações intencionais de IDs exigirão aliases/migração explícita futura;
- não foram executados builds Android/iOS nem smoke test em dispositivo;
- a branch foi criada sobre alterações não commitadas preexistentes da Task 002,
  que foram preservadas e permanecem no working tree.

## Não implementado

Inventário funcional, crafting runtime, progressão de herói, batalha, gacha,
backend, mercado, UI final, banco de dados, SDK externo e pipeline complexo de
importação não fazem parte desta tarefa.

## Próximos passos

A próxima task recomendada é introduzir um manifesto versionado de catálogo e
um importador simples, determinístico e validado para JSON/CSV, incluindo diff
de IDs removidos/alterados e hash de conteúdo. Isso permite escalar autoria sem
transformar o cliente em autoridade e sem acoplar o pipeline a cenas.
