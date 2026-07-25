# Modelo de segurança

## Objetivos e limites

Proteger identidade, propriedade, moeda, disponibilidade, privacidade e trilha
de auditoria. Nenhuma tecnologia impede fraude completamente. Controles
reduzem probabilidade/impacto, aumentam custo do atacante e melhoram detecção e
resposta.

App Check, Play Integrity, DeviceCheck ou App Attest não substituem
autenticação, autorização, idempotência, constraints ou regras de negócio. A
própria documentação do Firebase afirma que App Check não elimina todo abuso:
<https://firebase.google.com/docs/app-check>. A Apple recomenda integrar sinais
de DeviceCheck/App Attest a uma avaliação mais ampla:
<https://developer.apple.com/documentation/devicecheck>.

## Autenticação e sessão

- Provedor de identidade emite prova inicial; backend valida issuer, audience,
  assinatura, expiração e nonce.
- API emite access token curto e refresh token opaco/rotacionável.
- Refresh tokens são armazenados somente como hash no servidor e no secure
  storage da plataforma; replay revoga a família.
- `playerId` vem do claim/sessão validada, nunca do body.
- `deviceSessionId` vincula token, dispositivo lógico, attestation e versão.
- Logout/revogação, ban, troca de senha/conta e suspeita encerram sessões.
- Guest upgrade/linking exige prova das duas identidades e é idempotente.

## Autorização

- Default deny.
- Jogador acessa apenas seus recursos privados; listagens públicas expõem
  projeção mínima, nunca e-mail, device/session ou ledger privado.
- Admin/support usa identidade separada, MFA, RBAC, motivo obrigatório e audit.
- Jobs/workers usam service identities de menor privilégio.
- Toda mutação revalida owner/state/eligibility no banco dentro da transação.

## Transporte e secrets

- TLS 1.2+; HSTS e configuração moderna de cipher na borda.
- Certificados e chaves têm rotação; pinning no mobile é decisão separada,
  considerando recuperação.
- Secrets ficam em secret manager/KMS, nunca no repositório, imagem, cliente,
  config pública, log ou analytics.
- Chaves por ambiente e princípio de menor privilégio.
- Banco não é exposto à internet pública; conexões usam TLS e credenciais
  rotacionáveis.

## Rate limit e abuso

Limites combinam conta, sessão, dispositivo, rota e sinal de rede, sem depender
apenas de IP. Políticas distintas:

- bootstrap/refresh: baixa taxa, proteção contra credential stuffing;
- gacha/market/buy/claims: burst pequeno e dedup obrigatório;
- browse/history: paginação e custo limitado;
- attestation challenge: nonce curto e uso único.

Responder `RATE_LIMITED`, `Retry-After` e correlation ID. Rate limit não
substitui validação e deve ser testado sob carga.

## App Attestation

- Android: Play Integrity; iOS: App Attest/DeviceCheck; Firebase App Check pode
  normalizar providers em arquitetura híbrida.
- Nonce/challenge de uso único vinculado a sessão, método, hash do request e
  expiração reduz replay.
- Falta/risco pode bloquear operações críticas, limitar valores ou exigir
  verificação adicional, com estratégia para dispositivos não suportados.
- Nunca usar attestation como prova de saldo, winner, seed ou propriedade.

## Compras, recibos e chargeback

Não há IAP nesta task. Implementação futura:

- recebe token/recibo opaco, valida server-to-server com a loja;
- verifica app/product/environment/account e transação única;
- concede via ledger idempotente por store transaction ID;
- não confia em preço, moeda, produto ou status enviado pelo cliente;
- registra refund/chargeback como evento e lançamento compensatório;
- separa origem de Gemas para política de gasto/hold sem saldo negativo
  silencioso;
- congela/limita mercado quando risco de chargeback exigir.

## Replay e manipulação de relógio

- IDs, idempotência, nonce, expiração, revision e contador de assertion
  combatem replay.
- Relógio do cliente é apenas diagnóstico. Energia, idle, crafting, banners,
  agenda e expiração usam UTC do servidor.
- Skew anormal vira sinal de risco, não cálculo econômico.
- Logs não revelam seed ou desafio reutilizável.

## Botting e fraude de gameplay

- Limites de cadência, duração plausível, sequência de progressão e telemetry
  versionada.
- Batalhas críticas são simuladas/verificadas pelo servidor; `winner=player`
  não é aceito.
- Gacha é integralmente servidor; cliente não escolhe seed/result/pity.
- Detecção usa regras explicáveis e score de risco; ban automático exige
  thresholds conservadores, apelação e auditoria.
- Honeypots ou dados de detecção não alteram odds publicadas.

## Mercado, colusão e contas alternativas

Sinais:

- ciclos de compra entre poucas contas;
- preços extremos versus mediana/Tier;
- concentração de volume, criação recente, device/payment overlap;
- fluxo de Gemas compradas seguido de transferência e chargeback;
- wash trading, self-dealing indireto e velocity anormal.

Controles:

- autocompra proibida e item em escrow;
- limites de listing/volume por maturidade/risco;
- hold de proceeds de alto risco configurável;
- taxa fixa e preço mínimo/máximo versionados;
- revisão manual/ban/rollback por lançamentos compensatórios;
- alt accounts não são detectadas por um único identificador de dispositivo;
  combinar sinais com cuidado de privacidade e falsos positivos.

## Logs, PII e privacidade

- Logs estruturados incluem timestamp, environment, route, command type,
  resultado, latência, `correlationId`, IDs opacos e códigos de erro.
- Nunca registrar bearer/refresh token, receipt completo, assertion, e-mail,
  seed, payload de PII ou connection string.
- Pseudonimizar IDs em analytics; separar suporte/auditoria de telemetria.
- Classificar dados, definir retenção/expurgo e atender acesso/exclusão conforme
  lei aplicável, preservando ledger exigido por obrigação legítima.
- Acesso a audit/PII é monitorado e revisado.

## Ban e resposta a incidente

Estados sugeridos: `Active`, `Restricted`, `MarketRestricted`, `Suspended`,
`Banned`, com motivo, ator, início/fim e evidência. A resposta:

1. limitar dano e revogar sessões;
2. preservar audit/ledger/outbox;
3. correlacionar operações;
4. aplicar compensação idempotente quando comprovada;
5. comunicar de forma segura;
6. registrar post-mortem e melhorar alertas.

Ban não apaga ledger nem reutiliza item IDs.

## Observabilidade

Métricas/alertas:

- auth/refresh failure, attestation por resultado e rate limit;
- latência/erro por comando e revision conflict;
- dedup hit/conflict/processing timeout;
- wallet delta por razão e divergência de reconciliação;
- outputs por job, pity/hard pity, gacha odds observadas;
- listing sold/cancelled/expired, self-purchase e preço anormal;
- outbox lag, retries/dead-letter e worker backlog;
- chargeback, compensações, bans e concentração de mercado.

Traces propagam `correlationId`; eventos carregam `causationId`. Dashboards não
expõem PII nem tornam analytics fonte de verdade.

## Ameaças e controles resumidos

| Ameaça | Controles principais | Risco residual |
|---|---|---|
| edição de cache | snapshots/revisões e servidor autoritativo | UI temporariamente divergente |
| retry/replay | dedup, nonce, resposta armazenada | abuso após TTL protegido por constraints naturais |
| item/moeda duplicados | transação, unique constraints, ledger, reconciliação | bugs de regra/migração |
| clock manipulation | server time | automação legítima/maliciosa ainda exige análise |
| client mod/bot | attestation, rate, verificação e sinais | dispositivos comprometidos e fazendas |
| mercado/alt/colusão | escrow, graph/velocity/risk/holds | falsos positivos e coordenação externa |
| insider/admin | RBAC, MFA, four-eyes, audit append-only | credencial privilegiada comprometida |

