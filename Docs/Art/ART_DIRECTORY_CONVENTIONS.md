# Convenções de diretórios de arte

## Princípio

Documentação e produção artística ficam fora de `Assets` até existirem arquivos realmente prontos para importação. Pacotes, planilhas, CSVs, prompts, concepts e relatórios não são assets Unity.

## Onde guardar

| Conteúdo | Diretório |
|---|---|
| Documentos canônicos | `Docs/Art` |
| Pacotes históricos e catálogos operacionais | `ArtProduction/IdleMedievalLegends_TaskNNN_*` |
| Concepts e referências em avaliação | `ArtProduction/Incoming/Concepts/<asset_id>` |
| Downloads brutos | `ArtProduction/Incoming/Downloads/<source-or-task>` |
| Candidatos selecionados | `ArtProduction/Approved/Candidates/<asset_id>` |
| Fontes de produção aprovadas | `ArtProduction/Approved/Source/<asset_id>` |
| Relatórios reproduzíveis locais | `ArtProduction/GeneratedReports` |
| Assets efetivamente importados | `Assets/_Game/Art/<Category>` |

As categorias Unity reservadas são `Characters`, `Equipment`, `Environments`, `Stations`, `Materials`, `VFX`, `UI` e `Incoming`. Elas devem ser criadas pela etapa que importar um asset real, junto com os respectivos `.meta`; a Task 020 não cria placeholders.

## Fluxo

```text
Incoming/Downloads
→ pacote versionado e validado
→ Incoming/Concepts
→ Approved/Candidates
→ Approved/Source
→ Assets/_Game/Art
```

Cada transição exige registro de versão e aprovação. Mover um arquivo para `Approved` não o transforma automaticamente em asset final, e um destino `Assets/...` no catálogo não comprova existência.

## Política de versões

- `asset_id` é único, imutável e nunca é reutilizado.
- Alterações de conteúdo usam versões `v001`, `v002` e assim por diante.
- Versões substituídas permanecem no histórico do Git ou em pasta de arquivo documentada.
- Uma versão diferente nunca substitui silenciosamente outra cópia com o mesmo nome.
- O manifesto registra a versão do pacote, a origem e o checksum disponível.
- Mudanças em catálogos exigem regenerar o índice e executar o validador.

## Política de nomes

- IDs: ASCII minúsculo em `snake_case`, iniciando por letra.
- Concepts: `CONCEPT_<asset_id>_v###.<ext>`.
- Fontes Meshy: `MESHY_<asset_id>_v###.<ext>`.
- Texturas: `T_<asset_id>_<map>_v###.<ext>`.
- Materiais Unity: `MAT_<asset_id>_<variant>.mat`.
- Prefabs Unity: `PF_<asset_id>.prefab`.
- Animações Unity: `ANIM_<set>_<clip>.anim`.
- Prompts existentes mantêm seus nomes e redação; não são renomeados apenas por preferência editorial.

## Política de ZIPs e downloads

- O ZIP recebido pode permanecer temporariamente em `Incoming/Downloads`.
- Depois da extração e verificação, preserve o conteúdo versionado e remova o ZIP duplicado, salvo exigência de auditoria registrada no manifesto.
- ZIPs dentro de um pacote extraído são falha de validação, salvo exceção documentada.
- Arquivos `.part`, `.crdownload` e `.download` não são versionados.

## Política de arquivos grandes e LFS

Use Git LFS para `FBX`, `GLB`, `PSD`, `TGA`, `EXR`, `WAV`, `MP4` e outros binários grandes aprovados. Não mova nem crie arquivos inexistentes apenas para ativar uma regra. Markdown, CSV, JSON, YAML, planilhas operacionais e prompts necessários continuam versionados normalmente.

Antes de adicionar um binário grande:

1. confirme que é necessário e aprovado;
2. confirme a regra em `.gitattributes`;
3. verifique o ponteiro LFS com `git check-attr`;
4. registre origem, licença, versão e `asset_id`.

## Arquivos temporários

Renders temporários, relatórios gerados, caches, downloads incompletos e arquivos de ponte temporária ficam fora do versionamento. O `.gitignore` usa regras restritas a `ArtProduction` para não ocultar documentos, prompts ou catálogos necessários.
