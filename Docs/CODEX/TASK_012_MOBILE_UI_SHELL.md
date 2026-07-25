# TASK 012 — Estrutura principal de interface mobile

Data: 2026-07-24  
Branch: `feat/mobile-ui-shell`  
Unity: `6000.5.4f1 (d550df8bd089)`

## Objetivo e fronteiras

Esta task cria uma shell de navegação mobile para organizar os vertical slices
existentes sem mover regras de domínio para a apresentação. A UI recebe
snapshots e propriedades de leitura dos serviços locais já existentes; ela não
calcula recompensas, altera saldos, escolhe seeds nem implementa mercado.

Não foram implementados backend, autenticação, compras, anúncios, Gemas reais
ou mercado P2P. `PlayerPrefs` é usado exclusivamente para preferências de
apresentação.

## Tecnologia escolhida

A tecnologia principal é **uGUI**.

A inspeção encontrou uGUI em Battle, Inventory, Crafting, Campaign e Dungeon,
com `Canvas`, `CanvasScaler`, `GraphicRaycaster`, `EventSystem` e
`InputSystemUIInputModule`. Não existem `UIDocument`, UXML ou USS no runtime.
O simulador de gacha da Task 011 usa uma janela de Editor/IMGUI e não constitui
uma tela mobile.

UI Toolkit não foi introduzido. Reescrever os vertical slices em outra
tecnologia adicionaria risco sem benefício nesta etapa. O plano de
compatibilidade mantém as cenas uGUI e seus controllers enquanto a shell
centraliza navegação, tema, safe area, overlays e preferências. A migração
interna dos layouts legados pode ocorrer tela a tela, preservando os contratos
dos serviços e presenters.

## Arquitetura

```text
App (DontDestroyOnLoad)
└── MobileUiBootstrap
    └── AppNavigationController
        ├── MobileUiEventSystem
        └── MobileUiShellCanvas
            ├── SafeArea
            │   ├── ReusableHeader
            │   ├── ScreenContent
            │   └── MainTabBar
            ├── LoadingOverlay
            ├── Modal / ConfirmationDialog
            └── Toast
```

Contratos principais:

- `NavigationRoute`: IDs explícitos e estáveis das rotas;
- `NavigationHistory`: histórico puro, sem dependência de Unity;
- `AppNavigationController`: navegação, carregamento de cenas, back e shell;
- `ScreenPresenter` e `ScreenView`: estado por tela e ciclo de ativação;
- `ModalService`: garante apenas um modal aberto;
- `ToastService`: fila de sucesso, aviso e erro;
- `AsyncActionGate`: impede navegação/clique assíncrono duplicado;
- `SafeAreaController`: converte `Screen.safeArea` para anchors normalizados;
- `ResponsiveLayoutController`: limita a largura útil em tablet;
- `MobileInputController`: trata back/escape pelo Input System;
- `UiTextService`: resolve chaves preparatórias para localização;
- `UiPreferenceService`: persiste somente preferências visuais/sonoras;
- `UiSoundHooks`: ponto de integração para áudio e vibração;
- `UiAccessibilitySettings`: música, efeitos, vibração, movimento, contraste,
  escala de texto, velocidade de batalha e idioma.

Carregamentos de cena exibem `LoadingOverlay`, bloqueiam uma segunda navegação
e restauram interação ao concluir ou falhar. `SceneManager.LoadSceneAsync` não
oferece cancelamento real depois que a ativação começou; por isso a operação
de troca de cena é curta e não apresenta um botão de cancelamento enganoso.

## Navegação

A barra inferior prioriza cinco destinos:

1. Home;
2. Battle;
3. Heroes;
4. Crafting;
5. More.

`More` contém Inventory, Campaign, Dungeons, Gacha, Market, Profile e Settings.
Campanha recebeu rota própria adicional porque já existe como tela distinta.

| Rota | Implementação | Estado atual |
|---|---|---|
| Home | shell | pronta |
| Battle | cena `Battle` | integrada |
| Heroes | shell | vazia/placeholder |
| Inventory | cena `Inventory` | integrada |
| Crafting | cena `Crafting` | integrada |
| Dungeons | cena `Dungeon` | integrada |
| Gacha | shell | bancada mobile indisponível; sem moeda real |
| Market | shell | bloqueada, “Em breve” |
| Profile | shell | placeholder |
| Settings | shell | pronta |
| Campaign | cena `Campaign` | integrada |
| More | shell | pronta |

O `AppNavigationController` mantém um único EventSystem ativo durante as trocas
de cena. EventSystems serializados nas cenas legadas são desativados em runtime
quando a shell persistente está presente. Isso mantém compatibilidade com o
carregamento isolado usado pelos testes antigos.

Uma rota cuja cena já está ativa apenas oculta as views da shell e reutiliza a
instância existente. Assim, abrir ou reselecionar Battle não reinicia uma luta
em andamento. Rotas de cena indisponíveis são rejeitadas antes de alterar o
histórico; falhas tardias também restauram exatamente o snapshot anterior de
navegação e sua view.

## Home

A Home usa `HomeScreenPresenter` e `HomeScreenViewModel`. O resumo contém:

- Poder da equipe;
- estágio atual;
- ouro;
- Energia;
- duração elegível do relatório offline pendente;
- quantidade de jobs ativos de crafting;
- acesso rápido à Battle;
- acesso rápido às recompensas via Campaign.

Os valores são somente leitura. `CurrencyDisplay` formata `long` e não oferece
API de débito ou crédito. Não há indicador de Gemas.

## Estados e componentes

Toda tela pode usar `Loading`, `Ready`, `Empty`, `Error` ou `Locked`.

Componentes uGUI reutilizáveis criados:

- `PrimaryButton`, `SecondaryButton` e `IconButton`;
- `CurrencyDisplay` e `ProgressBar`;
- `RarityBadge` e `TierBadge`;
- `ItemCard` e `HeroCard`, que recebem view data;
- `EmptyState`, `ErrorState` e `LockedFeatureCard`;
- `LoadingSpinner` e `LoadingOverlay`;
- `TabBar`;
- `Modal`, `ConfirmationDialog` e `Toast`.

Toasts possuem severidade de sucesso, aviso e erro. Erros críticos permanecem
visíveis na tela/modal; toast não é a única fonte de informação crítica.
Retirar uma mensagem da fila não publica outro evento, evitando reentrância e
garantindo que mensagens acumuladas sejam exibidas uma vez e na ordem.

## Tema e responsividade

`UiThemeConfig.asset` centraliza:

- fonte primária opcional e escala tipográfica;
- cores de fundo, superfícies, texto, ação e overlay;
- cores de sucesso, aviso, erro e bloqueio;
- cores das seis raridades;
- espaçamento, raio, escalas compacta/expandida;
- tamanho mínimo de toque de 48 unidades de referência.

A shell usa `CanvasScaler.ScaleWithScreenSize` com referência `1080 × 1920` e
match 0,5. Layouts usam anchors e layout groups. A safe area é recalculada
quando resolução ou recorte mudam. Em proporções de tablet, o conteúdo recebe
margens laterais para evitar linhas excessivamente largas.

Perfis automatizados cobrem notch, 16:9, 19.5:9, 20:9 e tablet. Isso valida
anchors e containment; não substitui inspeção visual nem teste em aparelho.

## Acessibilidade e Settings

Settings permite configurar:

- música;
- efeitos de UI;
- vibração;
- velocidade de batalha 1x, 2x ou 3x;
- redução de movimento;
- alto contraste;
- escala de texto 0,8x a 1,4x;
- idioma como placeholder;
- conta como placeholder.

Escala de texto e alto contraste são aplicados à shell. `LoadingSpinner`
respeita redução de movimento. A vibração só é chamada em Android/iOS quando
habilitada. A velocidade salva é enviada à apresentação de Battle quando a
cena abre. Clipes e mixer de áudio ainda precisam de assets autorados.

## Integração e autoridade

As cenas legadas continuam sendo adapters dos serviços das Tasks 006 a 010.
A shell não duplica regras de combate, inventário, crafting, campanha ou
dungeon. Rotas carregam os mesmos scenes/controllers e preservam seus testes.

Os controllers legados ainda possuem construção programática própria e alguns
recebem agregados/serviços locais mutáveis. Nesta task eles ficam atrás das
rotas de cena por compatibilidade; novos screens da shell usam
presenter/view model. A migração de cada controller legado para portas de
intenção e view models imutáveis é trabalho incremental, não uma justificativa
para reimplementar domínio nesta task.

Gacha permanece somente uma bancada de Editor. A rota mobile informa essa
limitação sem expor `DevelopmentGachaCurrency`. Market apresenta estado
`Locked` e explica que o recurso dependerá de conexão e autoridade do servidor;
nenhuma promessa de disponibilidade ou operação de mercado foi criada.

## Validação de Editor

Menu:

**Tools > Idle Medieval Legends > UI > Generate or Update Mobile Shell**

**Tools > Idle Medieval Legends > UI > Validate Mobile Shell**

O gerador cria/atualiza o tema, adiciona exatamente um `MobileUiBootstrap` ao
objeto `App` e salva a cena pelas APIs do Editor.

O validador detecta:

- rota duplicada;
- rota sem cena/screen;
- destino sem chave;
- cena sem controller/presenter reconhecido;
- Canvas ScreenSpaceOverlay duplicado inadequado;
- EventSystem duplicado;
- bootstrap/tema/safe-area runtime ausente.

Canvas legados compatíveis podem ser reportados como aviso em vez de serem
apagados automaticamente.

Ao ser executado pelo menu, o validador solicita que alterações de cena sejam
salvas ou descartadas antes da inspeção. A configuração completa de cenas
abertas/ativas é capturada e restaurada em `finally`. Chamadas programáticas
sobre uma cena ainda suja são rejeitadas sem descarregar a cena. Em batchmode,
setups vazios sem cena ativa são reconhecidos e não são enviados à API de
restauração inválida do Unity.

## Testes e resultados

EditMode cobre histórico, back, raiz do histórico, rota inválida, modal único,
prevenção de duplo clique, formatação de moedas, raridades/tema e round-trip de
preferências sem chaves econômicas.

PlayMode cobre:

- Bootstrap pronto abrindo Home;
- navegação e back;
- abertura, confirmação e fechamento de modal;
- Market bloqueado;
- safe area e responsividade nos perfis pedidos;
- acesso a todas as rotas;
- ausência de exceções inesperadas durante a jornada.

Resultados finais no Unity `6000.5.4f1`:

- compilação/importação: código 0, sem `error CS`, `warning CS`,
  `NullReferenceException` ou `MissingReferenceException`;
- EditMode: `291 total, 291 passed, 0 failed, 0 skipped`;
- PlayMode: `21 total, 21 passed, 0 failed, 0 skipped`;
- validação específica da shell: código 0, zero erros e zero avisos;
- validação estrutural do projeto: código 0, projeto válido e somente o aviso
  preexistente de `DefaultCompany`;
- builds Android/iOS e smoke tests em dispositivo/simulador: não executados.

Logs e XMLs:

```text
%TEMP%/IdleMedievalLegends-validation-task012
```

## Limitações e pendências genuínas

- Arte, ícones, fontes, animações, áudio e haptics definitivos não foram
  autorados.
- Heroes e Profile são placeholders; Gacha runtime continua indisponível.
- As telas legadas foram desenhadas originalmente para 1920 × 1080 e ainda
  precisam de migração visual interna para os componentes/tema da shell.
- A shell usa português preparatório por chaves, mas não há catálogo de
  localização completo.
- Não houve backend, login, mercado, Gemas, IAP ou anúncios.
- Builds Android/iOS, device smoke tests e inspeção manual de notch real
  permanecem pendentes.
- Nenhum commit é criado automaticamente por esta task.
