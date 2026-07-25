# TASK 016 — Fichas detalhadas de heróis, inimigos e bosses

## Branch

```text
art/task-016-character-enemy-boss-design
```

## Prompt para o Codex

```text
TASK 016 — Integrar as fichas de personagens, inimigos e bosses

Antes de editar:
1. Leia AGENTS.md.
2. Leia Docs/Art/IDLE_MEDIEVAL_LEGENDS_VISUAL_BIBLE.md.
3. Leia Docs/Art/IDLE_MEDIEVAL_LEGENDS_ASSET_PRODUCTION_STANDARD.md.
4. Leia Docs/Art/IDLE_MEDIEVAL_LEGENDS_CHARACTER_DESIGN_BIBLE.md.
5. Leia Docs/Art/RIG_AND_SOCKET_STANDARD.md.
6. Inspecione Examples/CHARACTER_PRODUCTION_SHEETS.csv.
7. Inspecione os prompts em Prompts/.
8. Inspecione o catálogo da Task 015.
9. Execute git status e apresente plano curto.

Objetivo:
Integrar ao projeto a especificação de produção dos três heróis iniciais e dos
nove membros da facção Goblin, sem gerar malhas ou importar SDK externo.

Escopo obrigatório:
- preservar todos os IDs estáveis;
- criar estrutura Docs/Art e Docs/CODEX quando ausente;
- copiar e referenciar o CSV de produção;
- registrar os perfis de rig como documentação e configuração futura;
- criar um CharacterArtDefinition ou equivalente somente se o projeto já tiver
  arquitetura compatível e a mudança for pequena;
- não duplicar HeroDefinition ou EnemyDefinition existentes;
- adicionar campos de referência artística somente como IDs/caminhos, sem acoplar
  o domínio a UnityEngine.Object;
- criar validação Editor para IDs, escala, rig profile, animation set, orçamento,
  textura e dependências;
- criar relatório que confirme 3 heróis e 9 goblins;
- verificar que armas e escudos possuem assets separados no catálogo;
- verificar que boss e mid-boss têm animation sets próprios;
- documentar qualquer divergência entre o catálogo e as fichas;
- criar Docs/CODEX/TASK_016_CHARACTER_ENEMY_BOSS_DESIGN.md.

Não fazer:
- não gerar imagens automaticamente;
- não chamar Meshy;
- não importar modelos;
- não criar rigs reais;
- não criar animações;
- não alterar gameplay ou balanceamento;
- não reordenar enums;
- não criar commit automaticamente.

Testes/validações:
- IDs únicos;
- escalas positivas;
- triângulos válidos;
- rig profile conhecido;
- animation set conhecido;
- prompts presentes para todos os 12 personagens;
- todos os props separados referenciados;
- nenhum personagem sem critério de aprovação;
- nenhuma referência de arquivo inexistente.

Ao terminar, liste:
- arquivos alterados;
- validações executadas;
- divergências encontradas;
- itens que dependem de geração de arte;
- confirmação de ausência de commit.
```
