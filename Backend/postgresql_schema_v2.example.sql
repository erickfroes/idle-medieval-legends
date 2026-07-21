-- Idle Medieval Legends — esquema relacional de referência v2
-- PostgreSQL 15+. Use migrations versionadas; não execute diretamente em produção.

CREATE TABLE player_profiles (
    player_id                   text PRIMARY KEY,
    account_power               bigint NOT NULL DEFAULT 0 CHECK (account_power >= 0),
    season_peak_power           bigint NOT NULL DEFAULT 0 CHECK (season_peak_power >= 0),
    primary_profession          text NULL CHECK (primary_profession IN
        ('blacksmith','tailor','enchanter','alchemist','gatherer')),
    crafting_focus_available    integer NOT NULL DEFAULT 100 CHECK (crafting_focus_available >= 0),
    crafting_focus_cap          integer NOT NULL DEFAULT 100 CHECK (crafting_focus_cap > 0),
    focus_updated_at            timestamptz NOT NULL DEFAULT now(),
    balance_config_version      integer NOT NULL,
    revision                    bigint NOT NULL DEFAULT 0,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    updated_at                  timestamptz NOT NULL DEFAULT now(),
    CHECK (crafting_focus_available <= crafting_focus_cap)
);

CREATE TABLE wallets (
    player_id                   text PRIMARY KEY REFERENCES player_profiles(player_id),
    gems_available              bigint NOT NULL DEFAULT 0 CHECK (gems_available >= 0),
    gems_held                   bigint NOT NULL DEFAULT 0 CHECK (gems_held >= 0),
    gold                        bigint NOT NULL DEFAULT 0 CHECK (gold >= 0),
    revision                    bigint NOT NULL DEFAULT 0,
    updated_at                  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE profession_progress (
    player_id                   text NOT NULL REFERENCES player_profiles(player_id),
    profession_id               text NOT NULL CHECK (profession_id IN
        ('blacksmith','tailor','enchanter','alchemist','gatherer')),
    level                       integer NOT NULL DEFAULT 1 CHECK (level BETWEEN 1 AND 100),
    total_experience            bigint NOT NULL DEFAULT 0 CHECK (total_experience >= 0),
    rank_id                     text NOT NULL DEFAULT 'apprentice' CHECK (rank_id IN
        ('apprentice','proficient','master','grandmaster','god')),
    max_unlocked_tier           smallint NOT NULL DEFAULT 1 CHECK (max_unlocked_tier BETWEEN 1 AND 9),
    station_tier                smallint NOT NULL DEFAULT 1 CHECK (station_tier BETWEEN 1 AND 9),
    crafts_completed            bigint NOT NULL DEFAULT 0 CHECK (crafts_completed >= 0),
    mastery_points              integer NOT NULL DEFAULT 0 CHECK (mastery_points >= 0),
    mythic_pity_counter         integer NOT NULL DEFAULT 0 CHECK (mythic_pity_counter >= 0),
    revision                    bigint NOT NULL DEFAULT 0,
    updated_at                  timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (player_id, profession_id)
);

CREATE TABLE recipe_unlocks (
    player_id                   text NOT NULL REFERENCES player_profiles(player_id),
    recipe_id                   text NOT NULL,
    unlock_source               text NOT NULL,
    source_instance_id          text NULL,
    catalog_version             integer NOT NULL,
    unlocked_at                 timestamptz NOT NULL DEFAULT now(),
    revision                    bigint NOT NULL DEFAULT 0,
    PRIMARY KEY (player_id, recipe_id)
);

CREATE TABLE item_instances (
    item_instance_id            text PRIMARY KEY,
    definition_id               text NOT NULL,
    owner_player_id             text NOT NULL REFERENCES player_profiles(player_id),
    kind                        text NOT NULL CHECK (kind IN
        ('material','refined_material','equipment','skin','diagram',
         'consumable','crafting_tool','enchantment','currency_mirror')),
    tier                        smallint NOT NULL CHECK (tier BETWEEN 1 AND 9),
    rarity                      text NOT NULL CHECK (rarity IN
        ('common','uncommon','rare','epic','legendary','mythic')),
    quantity                    bigint NOT NULL CHECK (quantity >= 0),
    stackable                   boolean NOT NULL,
    state                       text NOT NULL CHECK (state IN
        ('owned','equipped','escrow','reserved','consumed','destroyed')),
    binding                     text NOT NULL CHECK (binding IN ('unbound','account','hero')),
    equipped_hero_instance_id   text NULL,
    listing_id                  text NULL,
    reservation_id              text NULL,
    source_profession           text NULL CHECK (source_profession IS NULL OR source_profession IN
        ('blacksmith','tailor','enchanter','alchemist','gatherer')),
    recipe_id                   text NULL,
    crafted_by_player_id        text NULL REFERENCES player_profiles(player_id),
    origin_transaction_id       text NOT NULL,
    parent_instance_id          text NULL REFERENCES item_instances(item_instance_id),
    quality_score_bps           integer NOT NULL DEFAULT 0 CHECK (quality_score_bps BETWEEN 0 AND 10000),
    enhancement_level           integer NOT NULL DEFAULT 0 CHECK (enhancement_level >= 0),
    roll_seed_hash              text NULL,
    rolled_stats                jsonb NOT NULL DEFAULT '[]'::jsonb,
    version                     bigint NOT NULL DEFAULT 0,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    updated_at                  timestamptz NOT NULL DEFAULT now(),
    CHECK (stackable OR quantity <= 1),
    CHECK (state IN ('consumed','destroyed') OR quantity > 0),
    CHECK (state <> 'equipped' OR equipped_hero_instance_id IS NOT NULL),
    CHECK (state <> 'escrow' OR listing_id IS NOT NULL),
    CHECK (state <> 'reserved' OR reservation_id IS NOT NULL),
    CHECK (recipe_id IS NULL OR
        (crafted_by_player_id IS NOT NULL AND source_profession IS NOT NULL AND roll_seed_hash IS NOT NULL))
);

CREATE INDEX item_owner_state_idx
    ON item_instances(owner_player_id, state, kind, tier);
CREATE INDEX item_reservation_idx
    ON item_instances(reservation_id)
    WHERE reservation_id IS NOT NULL;
CREATE INDEX item_market_filter_idx
    ON item_instances(kind, tier, rarity)
    WHERE state = 'escrow';

CREATE TABLE craft_jobs (
    job_id                      text PRIMARY KEY,
    player_id                   text NOT NULL REFERENCES player_profiles(player_id),
    recipe_id                   text NOT NULL,
    profession_id               text NOT NULL CHECK (profession_id IN
        ('blacksmith','tailor','enchanter','alchemist','gatherer')),
    quantity                    integer NOT NULL CHECK (quantity > 0),
    status                      text NOT NULL CHECK (status IN
        ('queued','in_progress','ready_to_finalize','completed','cancelled','failed')),
    reservation_id              text NOT NULL UNIQUE,
    catalog_version             integer NOT NULL,
    balance_config_version      integer NOT NULL,
    tool_instance_id            text NULL,
    catalyst_instance_id        text NULL,
    output_owner_player_id      text NOT NULL REFERENCES player_profiles(player_id),
    request_id                  text NOT NULL,
    started_at                  timestamptz NOT NULL,
    completes_at                timestamptz NOT NULL,
    completed_at                timestamptz NULL,
    result_json                 jsonb NULL,
    version                     bigint NOT NULL DEFAULT 0,
    UNIQUE (player_id, request_id),
    CHECK (completes_at >= started_at)
);

CREATE TABLE craft_job_inputs (
    job_id                      text NOT NULL REFERENCES craft_jobs(job_id),
    input_ordinal               integer NOT NULL CHECK (input_ordinal >= 0),
    item_instance_id            text NOT NULL REFERENCES item_instances(item_instance_id),
    quantity_consumed           bigint NOT NULL CHECK (quantity_consumed > 0),
    item_version_at_reservation bigint NOT NULL,
    PRIMARY KEY (job_id, input_ordinal)
);

CREATE TABLE craft_outputs (
    job_id                      text NOT NULL REFERENCES craft_jobs(job_id),
    output_index                integer NOT NULL CHECK (output_index >= 0),
    item_instance_id            text NOT NULL UNIQUE REFERENCES item_instances(item_instance_id),
    PRIMARY KEY (job_id, output_index)
);

CREATE TABLE craft_transactions (
    craft_transaction_id        text PRIMARY KEY,
    job_id                      text NOT NULL UNIQUE REFERENCES craft_jobs(job_id),
    player_id                   text NOT NULL REFERENCES player_profiles(player_id),
    profession_id               text NOT NULL,
    profession_xp_granted       bigint NOT NULL CHECK (profession_xp_granted >= 0),
    completed_at                timestamptz NOT NULL,
    payload                     jsonb NOT NULL
);

CREATE TABLE item_provenance (
    item_instance_id            text PRIMARY KEY REFERENCES item_instances(item_instance_id),
    origin_type                 text NOT NULL CHECK (origin_type IN
        ('craft','drop','gacha','quest','market_transfer','admin_grant','migration')),
    origin_transaction_id       text NOT NULL,
    job_id                      text NULL REFERENCES craft_jobs(job_id),
    recipe_id                   text NULL,
    crafter_player_id           text NULL REFERENCES player_profiles(player_id),
    catalog_version             integer NULL,
    balance_config_version      integer NOT NULL,
    input_snapshot              jsonb NOT NULL DEFAULT '[]'::jsonb,
    roll_seed_hash              text NULL,
    created_at                  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE market_listings (
    listing_id                  text PRIMARY KEY,
    item_instance_id            text NOT NULL REFERENCES item_instances(item_instance_id),
    seller_player_id            text NOT NULL REFERENCES player_profiles(player_id),
    buyer_player_id             text NULL REFERENCES player_profiles(player_id),
    item_kind_snapshot          text NOT NULL,
    tier_snapshot               smallint NOT NULL CHECK (tier_snapshot BETWEEN 1 AND 9),
    rarity_snapshot             text NOT NULL,
    price_gems                  bigint NOT NULL CHECK (price_gems >= 10),
    fee_basis_points            integer NOT NULL DEFAULT 1000 CHECK (fee_basis_points BETWEEN 0 AND 10000),
    fee_gems                    bigint NULL CHECK (fee_gems IS NULL OR fee_gems >= 0),
    seller_net_gems             bigint NULL CHECK (seller_net_gems IS NULL OR seller_net_gems >= 0),
    status                      text NOT NULL CHECK (status IN
        ('pending','active','reserved','sold','cancelled','expired','failed')),
    request_id                  text NOT NULL,
    transaction_id              text NULL,
    expires_at                  timestamptz NOT NULL,
    version                     bigint NOT NULL DEFAULT 0,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    updated_at                  timestamptz NOT NULL DEFAULT now(),
    UNIQUE (seller_player_id, request_id)
);

CREATE UNIQUE INDEX one_active_listing_per_item_idx
    ON market_listings(item_instance_id)
    WHERE status IN ('pending','active','reserved');
CREATE INDEX market_browse_idx
    ON market_listings(status, item_kind_snapshot, tier_snapshot, rarity_snapshot, price_gems)
    WHERE status = 'active';

CREATE TABLE crafting_commissions (
    commission_id               text PRIMARY KEY,
    buyer_player_id             text NOT NULL REFERENCES player_profiles(player_id),
    crafter_player_id           text NULL REFERENCES player_profiles(player_id),
    recipe_id                   text NOT NULL,
    profession_id               text NOT NULL,
    quantity                    integer NOT NULL CHECK (quantity > 0),
    ingredient_reservation_id   text NOT NULL UNIQUE,
    service_fee_gems            bigint NOT NULL CHECK (service_fee_gems >= 0),
    fee_basis_points            integer NOT NULL DEFAULT 1000 CHECK (fee_basis_points BETWEEN 0 AND 10000),
    status                      text NOT NULL CHECK (status IN
        ('open','accepted','crafting','completed','cancelled','expired','failed')),
    job_id                      text NULL UNIQUE REFERENCES craft_jobs(job_id),
    buyer_request_id            text NOT NULL,
    crafter_request_id          text NULL,
    expires_at                  timestamptz NOT NULL,
    version                     bigint NOT NULL DEFAULT 0,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    updated_at                  timestamptz NOT NULL DEFAULT now(),
    UNIQUE (buyer_player_id, buyer_request_id),
    CHECK (buyer_player_id <> crafter_player_id)
);

CREATE TABLE market_transactions (
    market_transaction_id       text PRIMARY KEY,
    listing_id                  text NOT NULL UNIQUE REFERENCES market_listings(listing_id),
    item_instance_id            text NOT NULL REFERENCES item_instances(item_instance_id),
    buyer_player_id             text NOT NULL REFERENCES player_profiles(player_id),
    seller_player_id            text NOT NULL REFERENCES player_profiles(player_id),
    gross_gems                  bigint NOT NULL CHECK (gross_gems >= 0),
    fee_gems                    bigint NOT NULL CHECK (fee_gems >= 0),
    seller_net_gems             bigint NOT NULL CHECK (seller_net_gems >= 0),
    completed_at                timestamptz NOT NULL,
    request_id                  text NOT NULL,
    CHECK (gross_gems = fee_gems + seller_net_gems),
    CHECK (buyer_player_id <> seller_player_id)
);

CREATE TABLE crafting_commission_transactions (
    commission_transaction_id   text PRIMARY KEY,
    commission_id               text NOT NULL UNIQUE REFERENCES crafting_commissions(commission_id),
    job_id                      text NOT NULL UNIQUE REFERENCES craft_jobs(job_id),
    buyer_player_id             text NOT NULL REFERENCES player_profiles(player_id),
    crafter_player_id           text NOT NULL REFERENCES player_profiles(player_id),
    gross_service_fee_gems      bigint NOT NULL CHECK (gross_service_fee_gems >= 0),
    fee_gems                    bigint NOT NULL CHECK (fee_gems >= 0),
    crafter_net_gems            bigint NOT NULL CHECK (crafter_net_gems >= 0),
    completed_at                timestamptz NOT NULL,
    CHECK (gross_service_fee_gems = fee_gems + crafter_net_gems),
    CHECK (buyer_player_id <> crafter_player_id)
);

CREATE TABLE wallet_ledger (
    entry_id                    text PRIMARY KEY,
    transaction_id             text NOT NULL,
    player_id                   text NOT NULL,
    currency_id                 text NOT NULL,
    delta                       bigint NOT NULL,
    reason                      text NOT NULL,
    counterparty_id             text NULL,
    balance_after               bigint NULL,
    request_id                  text NULL,
    created_at                  timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX wallet_ledger_player_idx
    ON wallet_ledger(player_id, created_at DESC);
CREATE UNIQUE INDEX wallet_ledger_transaction_leg_idx
    ON wallet_ledger(transaction_id, player_id, reason);

CREATE TABLE command_deduplication (
    scope_key                   text PRIMARY KEY,
    command_type                text NOT NULL,
    status                      text NOT NULL CHECK (status IN
        ('processing','completed','failed_retryable')),
    result_json                 jsonb NULL,
    expires_at                  timestamptz NOT NULL,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    updated_at                  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE outbox_events (
    event_id                    text PRIMARY KEY,
    aggregate_type              text NOT NULL,
    aggregate_id                text NOT NULL,
    event_type                  text NOT NULL,
    payload                     jsonb NOT NULL,
    occurred_at                 timestamptz NOT NULL DEFAULT now(),
    published_at                timestamptz NULL,
    attempts                    integer NOT NULL DEFAULT 0 CHECK (attempts >= 0)
);
CREATE INDEX outbox_pending_idx
    ON outbox_events(occurred_at)
    WHERE published_at IS NULL;
