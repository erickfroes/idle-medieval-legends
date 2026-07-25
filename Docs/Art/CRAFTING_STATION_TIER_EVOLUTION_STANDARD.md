# Idle Medieval Legends — Crafting Station Tier Evolution Standard

## Princípio

As estações existem para comunicar progressão profissional. Cada família mantém sua função, silhueta, direção de interação e componentes essenciais entre T1 e T9.

## Marcos produzidos

```text
T1 → T3 → T5 → T7 → T9
```

T2, T4, T6 e T8 não exigem nova malha nesta fase.

## Regras de evolução

### T1

Pequena, manual, de fronteira e construída com materiais comuns.

### T3

Profissional, reforçada e com primeiro suporte rúnico.

### T5

Oficina de Mestre, com aparato especializado e materiais ancestrais.

### T7

Integra engenharia astral, levitação controlada e precisão mágica.

### T9

Estação de assinatura do grau Deus, com núcleo de criação e linguagem primordial.

## Footprint e interação

- footprint recomendado: 4m × 4m;
- frente: +Z;
- área humana livre: mínimo aproximado de 1,2m;
- output spawn não pode ficar dentro do collider;
- a câmera deve reconhecer o foco profissional em 3/4;
- partes animadas são objetos separados.

## Sockets

```text
INTERACT_WORK
OUTPUT_SPAWN
CAMERA_FOCUS
AUDIO_WORK
VFX_PRIMARY
VFX_SECONDARY
```

Mais um socket específico por profissão:

```text
WORK_ANVIL
WORK_BENCH
WORK_RUNE_TABLE
WORK_CAULDRON
WORK_MAP_TABLE
```

## O que não evolui

- profissão da estação;
- orientação do work point;
- família visual;
- função dos componentes essenciais;
- nomes dos sockets.

## O que evolui

- qualidade estrutural;
- material;
- organização;
- mecanismos;
- número de attachments permitidos;
- precisão e intensidade localizada da magia;
- prestígio da silhueta.

## VFX

Fogo, líquido, fumaça, levitação, runas animadas e revelação do item são implementados no Unity. A estação deve continuar legível quando todos estiverem desligados.
