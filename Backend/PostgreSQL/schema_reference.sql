-- Idle Medieval Legends - PostgreSQL schema reference for backend contracts v1.
-- REFERENCE ONLY. This is not a production migration and must not be executed
-- unchanged in production. Use reviewed, versioned migrations in the backend.
-- Target for evaluation: PostgreSQL 15+.

BEGIN;

CREATE TABLE players (
    player_id uuid PRIMARY KEY,
    status smallint NOT NULL DEFAULT 0 CHECK (status BETWEEN 0 AND 3),
    global_revision bigint NOT NULL DEFAULT 0 CHECK (global_revision >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz NULL
);
CREATE INDEX players_status_idx ON players (status) WHERE deleted_at IS NULL;

CREATE TABLE player_profiles (
    player_id uuid PRIMARY KEY REFERENCES players(player_id),
    display_name text NOT NULL DEFAULT '',
    locale text NOT NULL DEFAULT 'pt-BR',
    time_zone text NOT NULL DEFAULT 'UTC',
    account_power bigint NOT NULL DEFAULT 0 CHECK (account_power >= 0),
    season_peak_power bigint NOT NULL DEFAULT 0 CHECK (season_peak_power >= 0),
    active_team_id uuid NULL,
    primary_profession_id smallint NULL
        CHECK (primary_profession_id IS NULL OR primary_profession_id BETWEEN 1 AND 5),
    catalog_version text NOT NULL,
    rules_version text NOT NULL,
    revision bigint NOT NULL DEFAULT 0 CHECK (revision >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX player_profiles_account_power_idx
    ON player_profiles (account_power DESC);

CREATE TABLE player_sessions (
    session_id uuid PRIMARY KEY,
    player_id uuid NOT NULL REFERENCES players(player_id),
    device_session_id text NOT NULL,
    identity_provider text NOT NULL,
    refresh_token_hash bytea NOT NULL,
    token_family_id uuid NOT NULL,
    platform smallint NOT NULL CHECK (platform BETWEEN 1 AND 3),
    client_version text NOT NULL,
    attestation_state smallint NOT NULL DEFAULT 0 CHECK (attestation_state BETWEEN 0 AND 3),
    risk_score_bps integer NOT NULL DEFAULT 0 CHECK (risk_score_bps BETWEEN 0 AND 10000),
    access_expires_at timestamptz NOT NULL,
    refresh_expires_at timestamptz NOT NULL,
    revoked_at timestamptz NULL,
    last_seen_at timestamptz NOT NULL DEFAULT now(),
    revision bigint NOT NULL DEFAULT 0 CHECK (revision >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (player_id, device_session_id),
    UNIQUE (refresh_token_hash),
    CHECK (refresh_expires_at >= access_expires_at)
);
CREATE INDEX player_sessions_player_active_idx
    ON player_sessions (player_id, refresh_expires_at)
    WHERE revoked_at IS NULL;
CREATE INDEX player_sessions_expiry_idx
    ON player_sessions (refresh_expires_at)
    WHERE revoked_at IS NULL;

CREATE TABLE feature_flags (
    flag_key text PRIMARY KEY,
    environment text NOT NULL,
    value_json jsonb NOT NULL,
    targeting_rules jsonb NOT NULL DEFAULT '{}'::jsonb,
    enabled boolean NOT NULL DEFAULT false,
    version bigint NOT NULL DEFAULT 0 CHECK (version >= 0),
    starts_at timestamptz NULL,
    ends_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz NULL,
    CHECK (ends_at IS NULL OR starts_at IS NULL OR ends_at >= starts_at)
);
CREATE INDEX feature_flags_active_idx
    ON feature_flags (environment, enabled, starts_at, ends_at)
    WHERE deleted_at IS NULL;

CREATE TABLE wallets (
    player_id uuid PRIMARY KEY REFERENCES players(player_id),
    gold bigint NOT NULL DEFAULT 0 CHECK (gold >= 0),
    gems_available bigint NOT NULL DEFAULT 0 CHECK (gems_available >= 0),
    gems_held bigint NOT NULL DEFAULT 0 CHECK (gems_held >= 0),
    revision bigint NOT NULL DEFAULT 0 CHECK (revision >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE wallet_ledger (
    entry_id uuid PRIMARY KEY,
    transaction_id uuid NOT NULL,
    leg_index smallint NOT NULL CHECK (leg_index >= 0),
    player_id uuid NULL REFERENCES players(player_id),
    system_account text NULL,
    currency_id text NOT NULL CHECK (currency_id IN ('gold', 'gems')),
    bucket text NOT NULL CHECK (bucket IN ('available', 'held', 'burn')),
    delta bigint NOT NULL CHECK (delta <> 0),
    balance_after bigint NULL CHECK (balance_after IS NULL OR balance_after >= 0),
    reason text NOT NULL,
    counterparty_player_id uuid NULL REFERENCES players(player_id),
    request_id text NULL,
    correlation_id text NOT NULL,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (transaction_id, leg_index),
    CHECK ((player_id IS NOT NULL AND system_account IS NULL) OR
           (player_id IS NULL AND system_account IS NOT NULL))
);
CREATE INDEX wallet_ledger_player_time_idx
    ON wallet_ledger (player_id, created_at DESC)
    WHERE player_id IS NOT NULL;
CREATE INDEX wallet_ledger_transaction_idx
    ON wallet_ledger (transaction_id);
COMMENT ON TABLE wallet_ledger IS
    'Append-only. Corrections are new compensating entries; application UPDATE/DELETE is forbidden.';

CREATE TABLE hero_instances (
    hero_instance_id uuid PRIMARY KEY,
    owner_player_id uuid NOT NULL REFERENCES players(player_id),
    hero_definition_id text NOT NULL,
    level integer NOT NULL DEFAULT 1 CHECK (level BETWEEN 1 AND 100),
    experience bigint NOT NULL DEFAULT 0 CHECK (experience >= 0),
    rarity smallint NOT NULL CHECK (rarity BETWEEN 0 AND 5),
    ascension smallint NOT NULL DEFAULT 0 CHECK (ascension BETWEEN 0 AND 5),
    unlocked boolean NOT NULL DEFAULT false,
    computed_power bigint NOT NULL DEFAULT 0 CHECK (computed_power >= 0),
    version bigint NOT NULL DEFAULT 0 CHECK (version >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz NULL,
    CHECK (unlocked OR (level = 1 AND experience = 0 AND ascension = 0))
);
CREATE INDEX hero_instances_owner_idx
    ON hero_instances (owner_player_id) WHERE deleted_at IS NULL;
CREATE INDEX hero_instances_owner_power_idx
    ON hero_instances (owner_player_id, computed_power DESC) WHERE unlocked;

CREATE TABLE hero_fragments (
    player_id uuid NOT NULL REFERENCES players(player_id),
    hero_definition_id text NOT NULL,
    balance bigint NOT NULL DEFAULT 0 CHECK (balance >= 0),
    revision bigint NOT NULL DEFAULT 0 CHECK (revision >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (player_id, hero_definition_id)
);

CREATE TABLE player_teams (
    team_id uuid PRIMARY KEY,
    player_id uuid NOT NULL REFERENCES players(player_id),
    team_type text NOT NULL,
    display_name text NOT NULL DEFAULT '',
    version bigint NOT NULL DEFAULT 0 CHECK (version >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz NULL
);
CREATE INDEX player_teams_player_idx
    ON player_teams (player_id) WHERE deleted_at IS NULL;

CREATE TABLE player_team_members (
    team_id uuid NOT NULL REFERENCES player_teams(team_id) ON DELETE CASCADE,
    slot_index smallint NOT NULL CHECK (slot_index BETWEEN 0 AND 4),
    hero_instance_id uuid NOT NULL REFERENCES hero_instances(hero_instance_id),
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (team_id, slot_index),
    UNIQUE (team_id, hero_instance_id)
);

ALTER TABLE player_profiles
    ADD CONSTRAINT player_profiles_active_team_fk
    FOREIGN KEY (active_team_id) REFERENCES player_teams(team_id);

CREATE TABLE item_instances (
    item_instance_id uuid PRIMARY KEY,
    definition_id text NOT NULL,
    owner_player_id uuid NOT NULL REFERENCES players(player_id),
    kind smallint NOT NULL CHECK (kind BETWEEN 0 AND 8),
    tier smallint NOT NULL CHECK (tier BETWEEN 1 AND 9),
    rarity smallint NOT NULL CHECK (rarity BETWEEN 0 AND 5),
    quantity bigint NOT NULL CHECK (quantity >= 0),
    stackable boolean NOT NULL,
    state smallint NOT NULL CHECK (state BETWEEN 0 AND 5),
    binding smallint NOT NULL CHECK (binding BETWEEN 0 AND 2),
    equipped_hero_instance_id uuid NULL REFERENCES hero_instances(hero_instance_id),
    listing_id uuid NULL,
    reservation_id uuid NULL,
    source_profession_id smallint NULL
        CHECK (source_profession_id IS NULL OR source_profession_id BETWEEN 1 AND 5),
    recipe_id text NULL,
    crafted_by_player_id uuid NULL REFERENCES players(player_id),
    origin_transaction_id uuid NOT NULL,
    parent_instance_id uuid NULL REFERENCES item_instances(item_instance_id),
    quality_score_bps integer NOT NULL DEFAULT 0
        CHECK (quality_score_bps BETWEEN 0 AND 10000),
    enhancement_level integer NOT NULL DEFAULT 0 CHECK (enhancement_level >= 0),
    roll_seed_hash bytea NULL,
    rolled_stats jsonb NOT NULL DEFAULT '[]'::jsonb,
    version bigint NOT NULL DEFAULT 0 CHECK (version >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz NULL,
    CHECK (stackable OR quantity <= 1),
    CHECK (state IN (4, 5) OR quantity > 0),
    CHECK (state <> 1 OR equipped_hero_instance_id IS NOT NULL),
    CHECK (state <> 2 OR listing_id IS NOT NULL),
    CHECK (state <> 3 OR reservation_id IS NOT NULL),
    CHECK (recipe_id IS NULL OR
          (crafted_by_player_id IS NOT NULL AND source_profession_id IS NOT NULL))
);
CREATE INDEX item_instances_owner_state_idx
    ON item_instances (owner_player_id, state, kind, tier)
    WHERE deleted_at IS NULL;
CREATE INDEX item_instances_reservation_idx
    ON item_instances (reservation_id) WHERE reservation_id IS NOT NULL;
CREATE INDEX item_instances_listing_idx
    ON item_instances (listing_id) WHERE listing_id IS NOT NULL;
CREATE INDEX item_instances_definition_idx
    ON item_instances (definition_id);

CREATE TABLE profession_progress (
    player_id uuid NOT NULL REFERENCES players(player_id),
    profession_id smallint NOT NULL CHECK (profession_id BETWEEN 1 AND 5),
    level integer NOT NULL DEFAULT 1 CHECK (level BETWEEN 1 AND 100),
    total_experience bigint NOT NULL DEFAULT 0 CHECK (total_experience >= 0),
    rank_id smallint NOT NULL DEFAULT 0 CHECK (rank_id BETWEEN 0 AND 4),
    max_unlocked_tier smallint NOT NULL DEFAULT 1
        CHECK (max_unlocked_tier BETWEEN 1 AND 9),
    station_tier smallint NOT NULL DEFAULT 1 CHECK (station_tier BETWEEN 1 AND 9),
    focus_available integer NOT NULL DEFAULT 100 CHECK (focus_available >= 0),
    focus_cap integer NOT NULL DEFAULT 100 CHECK (focus_cap > 0),
    focus_updated_at timestamptz NOT NULL DEFAULT now(),
    mastery_experience bigint NOT NULL DEFAULT 0 CHECK (mastery_experience >= 0),
    mastery_points integer NOT NULL DEFAULT 0 CHECK (mastery_points >= 0),
    mythic_pity_counter integer NOT NULL DEFAULT 0 CHECK (mythic_pity_counter >= 0),
    specialization_selected boolean NOT NULL DEFAULT false,
    specialization_cooldown_until timestamptz NULL,
    revision bigint NOT NULL DEFAULT 0 CHECK (revision >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (player_id, profession_id),
    CHECK (focus_available <= focus_cap)
);

CREATE TABLE crafting_jobs (
    job_id uuid PRIMARY KEY,
    player_id uuid NOT NULL REFERENCES players(player_id),
    output_owner_player_id uuid NOT NULL REFERENCES players(player_id),
    recipe_id text NOT NULL,
    profession_id smallint NOT NULL CHECK (profession_id BETWEEN 1 AND 5),
    quantity integer NOT NULL CHECK (quantity > 0),
    status smallint NOT NULL CHECK (status BETWEEN 0 AND 5),
    reservation_id uuid NOT NULL UNIQUE,
    tool_instance_id uuid NULL REFERENCES item_instances(item_instance_id),
    catalyst_instance_id uuid NULL REFERENCES item_instances(item_instance_id),
    catalog_version text NOT NULL,
    rules_version text NOT NULL,
    request_id text NOT NULL,
    started_at timestamptz NOT NULL,
    completes_at timestamptz NOT NULL,
    claimed_at timestamptz NULL,
    result_json jsonb NULL,
    version bigint NOT NULL DEFAULT 0 CHECK (version >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (player_id, request_id),
    CHECK (completes_at >= started_at)
);
CREATE INDEX crafting_jobs_player_status_idx
    ON crafting_jobs (player_id, status, completes_at);
CREATE INDEX crafting_jobs_worker_idx
    ON crafting_jobs (completes_at)
    WHERE status IN (1, 2);

CREATE TABLE crafting_job_inputs (
    job_id uuid NOT NULL REFERENCES crafting_jobs(job_id),
    input_index integer NOT NULL CHECK (input_index >= 0),
    item_instance_id uuid NOT NULL REFERENCES item_instances(item_instance_id),
    role smallint NOT NULL CHECK (role BETWEEN 0 AND 2),
    quantity_reserved bigint NOT NULL CHECK (quantity_reserved > 0),
    quantity_consumed bigint NOT NULL DEFAULT 0 CHECK (quantity_consumed >= 0),
    item_version_at_reservation bigint NOT NULL CHECK (item_version_at_reservation >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (job_id, input_index),
    CHECK (quantity_consumed <= quantity_reserved)
);
CREATE INDEX crafting_job_inputs_item_idx
    ON crafting_job_inputs (item_instance_id);

CREATE TABLE crafting_job_outputs (
    job_id uuid NOT NULL REFERENCES crafting_jobs(job_id),
    output_index integer NOT NULL CHECK (output_index >= 0),
    item_instance_id uuid NOT NULL UNIQUE REFERENCES item_instances(item_instance_id),
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (job_id, output_index)
);

CREATE TABLE campaign_progress (
    player_id uuid PRIMARY KEY REFERENCES players(player_id),
    current_stage_id text NULL,
    highest_cleared_stage_id text NULL,
    accumulated_experience bigint NOT NULL DEFAULT 0
        CHECK (accumulated_experience >= 0),
    last_session_start_at timestamptz NULL,
    last_claimed_at timestamptz NULL,
    pending_offline_report_id uuid NULL,
    revision bigint NOT NULL DEFAULT 0 CHECK (revision >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE campaign_first_clears (
    player_id uuid NOT NULL REFERENCES players(player_id),
    stage_id text NOT NULL,
    battle_id uuid NOT NULL,
    reward_transaction_id uuid NOT NULL UNIQUE,
    completed_at timestamptz NOT NULL,
    claimed_at timestamptz NOT NULL,
    PRIMARY KEY (player_id, stage_id)
);

CREATE TABLE offline_reward_reports (
    report_id uuid PRIMARY KEY,
    player_id uuid NOT NULL REFERENCES players(player_id),
    stage_id text NULL,
    period_started_at timestamptz NOT NULL,
    period_ended_at timestamptz NOT NULL,
    eligible_duration_seconds bigint NOT NULL CHECK (eligible_duration_seconds >= 0),
    catalog_version text NOT NULL,
    rules_version text NOT NULL,
    reward_json jsonb NOT NULL,
    status smallint NOT NULL DEFAULT 0 CHECK (status BETWEEN 0 AND 2),
    claim_transaction_id uuid NULL UNIQUE,
    version bigint NOT NULL DEFAULT 0 CHECK (version >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK (period_ended_at >= period_started_at),
    CHECK (status <> 1 OR claim_transaction_id IS NOT NULL)
);
CREATE UNIQUE INDEX offline_reward_one_pending_idx
    ON offline_reward_reports (player_id) WHERE status = 0;
ALTER TABLE campaign_progress
    ADD CONSTRAINT campaign_progress_pending_report_fk
    FOREIGN KEY (pending_offline_report_id) REFERENCES offline_reward_reports(report_id);

CREATE TABLE energy_wallets (
    player_id uuid PRIMARY KEY REFERENCES players(player_id),
    current_energy integer NOT NULL CHECK (current_energy >= 0),
    maximum_energy integer NOT NULL CHECK (maximum_energy > 0),
    regeneration_interval_seconds integer NOT NULL
        CHECK (regeneration_interval_seconds > 0),
    regeneration_anchor_at timestamptz NOT NULL,
    revision bigint NOT NULL DEFAULT 0 CHECK (revision >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK (current_energy <= maximum_energy)
);

CREATE TABLE dungeon_runs (
    run_id uuid PRIMARY KEY,
    player_id uuid NOT NULL REFERENCES players(player_id),
    team_id uuid NOT NULL REFERENCES player_teams(team_id),
    dungeon_id text NOT NULL,
    difficulty_id text NOT NULL,
    state smallint NOT NULL CHECK (state BETWEEN 0 AND 7),
    energy_cost integer NOT NULL CHECK (energy_cost >= 0),
    attempt_date date NOT NULL,
    request_id text NOT NULL,
    catalog_version text NOT NULL,
    rules_version text NOT NULL,
    battle_reference text NULL,
    battle_result_hash text NULL,
    outcome smallint NULL CHECK (outcome IS NULL OR outcome BETWEEN 0 AND 2),
    first_clear boolean NOT NULL DEFAULT false,
    reward_json jsonb NULL,
    started_at timestamptz NOT NULL,
    completed_at timestamptz NULL,
    claimed_at timestamptz NULL,
    version bigint NOT NULL DEFAULT 0 CHECK (version >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (player_id, request_id)
);
CREATE INDEX dungeon_runs_player_state_idx
    ON dungeon_runs (player_id, state, started_at DESC);

CREATE TABLE dungeon_claims (
    run_id uuid PRIMARY KEY REFERENCES dungeon_runs(run_id),
    reward_transaction_id uuid NOT NULL UNIQUE,
    result_json jsonb NOT NULL,
    claimed_at timestamptz NOT NULL
);

CREATE TABLE gacha_pulls (
    pull_id uuid PRIMARY KEY,
    player_id uuid NOT NULL REFERENCES players(player_id),
    banner_id text NOT NULL,
    quantity integer NOT NULL CHECK (quantity > 0),
    currency_id text NOT NULL,
    cost bigint NOT NULL CHECK (cost >= 0),
    request_id text NOT NULL,
    catalog_version text NOT NULL,
    rules_version text NOT NULL,
    result_json jsonb NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (player_id, request_id)
);

CREATE TABLE gacha_pity (
    player_id uuid NOT NULL REFERENCES players(player_id),
    pity_group_id text NOT NULL,
    track_id text NOT NULL,
    pulls_since_hit integer NOT NULL DEFAULT 0 CHECK (pulls_since_hit >= 0),
    featured_guarantee boolean NOT NULL DEFAULT false,
    total_pulls bigint NOT NULL DEFAULT 0 CHECK (total_pulls >= 0),
    revision bigint NOT NULL DEFAULT 0 CHECK (revision >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (player_id, pity_group_id, track_id)
);

CREATE TABLE gacha_history (
    history_id uuid PRIMARY KEY,
    pull_id uuid NOT NULL REFERENCES gacha_pulls(pull_id),
    player_id uuid NOT NULL REFERENCES players(player_id),
    banner_id text NOT NULL,
    sequence integer NOT NULL CHECK (sequence >= 0),
    reward_type text NOT NULL,
    reward_definition_id text NOT NULL,
    rarity smallint NOT NULL CHECK (rarity BETWEEN 0 AND 5),
    quantity bigint NOT NULL CHECK (quantity > 0),
    pity_before jsonb NOT NULL,
    pity_after jsonb NOT NULL,
    catalog_version text NOT NULL,
    rules_version text NOT NULL,
    correlation_id text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (pull_id, sequence)
);
CREATE INDEX gacha_history_player_time_idx
    ON gacha_history (player_id, created_at DESC);
CREATE INDEX gacha_history_player_banner_idx
    ON gacha_history (player_id, banner_id, created_at DESC);
COMMENT ON TABLE gacha_history IS
    'Immutable authoritative gacha history. Application UPDATE/DELETE is forbidden.';

CREATE TABLE market_listings (
    listing_id uuid PRIMARY KEY,
    item_instance_id uuid NOT NULL REFERENCES item_instances(item_instance_id),
    seller_player_id uuid NOT NULL REFERENCES players(player_id),
    buyer_player_id uuid NULL REFERENCES players(player_id),
    item_kind_snapshot smallint NOT NULL,
    tier_snapshot smallint NOT NULL CHECK (tier_snapshot BETWEEN 1 AND 9),
    rarity_snapshot smallint NOT NULL CHECK (rarity_snapshot BETWEEN 0 AND 5),
    price_gems bigint NOT NULL CHECK (price_gems > 0),
    fee_basis_points integer NOT NULL DEFAULT 1000
        CHECK (fee_basis_points BETWEEN 0 AND 10000),
    fee_gems bigint NULL CHECK (fee_gems IS NULL OR fee_gems >= 0),
    seller_net_gems bigint NULL CHECK (seller_net_gems IS NULL OR seller_net_gems >= 0),
    status smallint NOT NULL DEFAULT 0 CHECK (status BETWEEN 0 AND 6),
    request_id text NOT NULL,
    transaction_id uuid NULL,
    expires_at timestamptz NOT NULL,
    version bigint NOT NULL DEFAULT 0 CHECK (version >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (seller_player_id, request_id),
    CHECK (buyer_player_id IS NULL OR buyer_player_id <> seller_player_id),
    CHECK (status <> 3 OR
          (buyer_player_id IS NOT NULL AND transaction_id IS NOT NULL AND
           fee_gems IS NOT NULL AND seller_net_gems IS NOT NULL))
);
CREATE UNIQUE INDEX market_one_active_listing_per_item_idx
    ON market_listings (item_instance_id) WHERE status IN (1, 2);
CREATE INDEX market_listings_browse_idx
    ON market_listings
       (status, item_kind_snapshot, tier_snapshot, rarity_snapshot, price_gems, listing_id)
    WHERE status = 1;
CREATE INDEX market_listings_seller_idx
    ON market_listings (seller_player_id, status, created_at DESC);
CREATE INDEX market_listings_expiry_idx
    ON market_listings (expires_at) WHERE status IN (1, 2);

ALTER TABLE item_instances
    ADD CONSTRAINT item_instances_listing_fk
    FOREIGN KEY (listing_id) REFERENCES market_listings(listing_id);

CREATE TABLE market_transactions (
    market_transaction_id uuid PRIMARY KEY,
    listing_id uuid NOT NULL UNIQUE REFERENCES market_listings(listing_id),
    item_instance_id uuid NOT NULL REFERENCES item_instances(item_instance_id),
    buyer_player_id uuid NOT NULL REFERENCES players(player_id),
    seller_player_id uuid NOT NULL REFERENCES players(player_id),
    gross_gems bigint NOT NULL CHECK (gross_gems >= 0),
    fee_gems bigint NOT NULL CHECK (fee_gems >= 0),
    seller_net_gems bigint NOT NULL CHECK (seller_net_gems >= 0),
    request_id text NOT NULL,
    correlation_id text NOT NULL,
    completed_at timestamptz NOT NULL,
    CHECK (buyer_player_id <> seller_player_id),
    CHECK (gross_gems = fee_gems + seller_net_gems)
);
CREATE INDEX market_transactions_buyer_idx
    ON market_transactions (buyer_player_id, completed_at DESC);
CREATE INDEX market_transactions_seller_idx
    ON market_transactions (seller_player_id, completed_at DESC);

CREATE TABLE command_deduplication (
    player_id uuid NOT NULL REFERENCES players(player_id),
    command_type text NOT NULL,
    idempotency_key text NOT NULL,
    command_id text NOT NULL,
    request_id text NOT NULL,
    payload_hash bytea NOT NULL,
    status smallint NOT NULL DEFAULT 0 CHECK (status BETWEEN 0 AND 2),
    http_status integer NULL CHECK (http_status IS NULL OR http_status BETWEEN 100 AND 599),
    response_json jsonb NULL,
    result_revision bigint NULL CHECK (result_revision IS NULL OR result_revision >= 0),
    row_revision bigint NOT NULL DEFAULT 0 CHECK (row_revision >= 0),
    expires_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (player_id, command_type, idempotency_key),
    UNIQUE (player_id, command_type, request_id),
    UNIQUE (player_id, command_type, command_id)
);
CREATE INDEX command_deduplication_expiry_idx
    ON command_deduplication (expires_at);
CREATE INDEX command_deduplication_processing_idx
    ON command_deduplication (updated_at) WHERE status = 0;

CREATE TABLE outbox_events (
    event_id uuid PRIMARY KEY,
    event_type text NOT NULL,
    aggregate_type text NOT NULL,
    aggregate_id text NOT NULL,
    aggregate_version bigint NOT NULL CHECK (aggregate_version >= 0),
    player_id uuid NULL REFERENCES players(player_id),
    payload jsonb NOT NULL,
    schema_version integer NOT NULL CHECK (schema_version > 0),
    correlation_id text NOT NULL,
    causation_id text NOT NULL,
    occurred_at timestamptz NOT NULL DEFAULT now(),
    available_at timestamptz NOT NULL DEFAULT now(),
    published_at timestamptz NULL,
    attempts integer NOT NULL DEFAULT 0 CHECK (attempts >= 0),
    last_error_code text NULL,
    UNIQUE (aggregate_type, aggregate_id, aggregate_version, event_type)
);
CREATE INDEX outbox_events_pending_idx
    ON outbox_events (available_at, occurred_at)
    WHERE published_at IS NULL;

CREATE TABLE audit_events (
    audit_event_id uuid PRIMARY KEY,
    player_id uuid NULL REFERENCES players(player_id),
    actor_type text NOT NULL,
    actor_id text NOT NULL,
    action text NOT NULL,
    target_type text NOT NULL,
    target_id text NOT NULL,
    result_code text NOT NULL,
    reason text NULL,
    correlation_id text NOT NULL,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX audit_events_target_idx
    ON audit_events (target_type, target_id, occurred_at DESC);
CREATE INDEX audit_events_actor_idx
    ON audit_events (actor_type, actor_id, occurred_at DESC);
CREATE INDEX audit_events_correlation_idx
    ON audit_events (correlation_id);
COMMENT ON TABLE audit_events IS
    'Append-only security/economic audit trail. Do not store secrets or unnecessary PII.';

COMMIT;

