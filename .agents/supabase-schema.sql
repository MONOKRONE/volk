-- VOLK Supabase Schema
-- Run this in Supabase SQL Editor

CREATE TABLE IF NOT EXISTS players (
    id UUID PRIMARY KEY DEFAULT auth.uid(),
    device_id TEXT UNIQUE NOT NULL,
    display_name TEXT DEFAULT 'Player',
    created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE IF NOT EXISTS save_data (
    player_id UUID PRIMARY KEY REFERENCES players(id),
    data JSONB NOT NULL DEFAULT '{}',
    updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE IF NOT EXISTS leaderboard (
    player_id UUID PRIMARY KEY REFERENCES players(id),
    score INTEGER DEFAULT 0,
    win_count INTEGER DEFAULT 0,
    streak INTEGER DEFAULT 0,
    updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE IF NOT EXISTS daily_quests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    player_id UUID REFERENCES players(id),
    quest_data JSONB NOT NULL DEFAULT '{}',
    assigned_date DATE DEFAULT CURRENT_DATE,
    completed BOOLEAN DEFAULT false,
    created_at TIMESTAMPTZ DEFAULT now()
);

-- Enable RLS
ALTER TABLE players ENABLE ROW LEVEL SECURITY;
ALTER TABLE save_data ENABLE ROW LEVEL SECURITY;
ALTER TABLE leaderboard ENABLE ROW LEVEL SECURITY;
ALTER TABLE daily_quests ENABLE ROW LEVEL SECURITY;

-- Policies: users can only access their own data
CREATE POLICY "Users can read own data" ON players FOR SELECT USING (auth.uid() = id);
CREATE POLICY "Users can insert own data" ON players FOR INSERT WITH CHECK (auth.uid() = id);
CREATE POLICY "Users can update own data" ON players FOR UPDATE USING (auth.uid() = id);

CREATE POLICY "Users can read own save" ON save_data FOR SELECT USING (auth.uid() = player_id);
CREATE POLICY "Users can upsert own save" ON save_data FOR INSERT WITH CHECK (auth.uid() = player_id);
CREATE POLICY "Users can update own save" ON save_data FOR UPDATE USING (auth.uid() = player_id);

-- Leaderboard: everyone can read, only own can write
CREATE POLICY "Anyone can read leaderboard" ON leaderboard FOR SELECT USING (true);
CREATE POLICY "Users can upsert own score" ON leaderboard FOR INSERT WITH CHECK (auth.uid() = player_id);
CREATE POLICY "Users can update own score" ON leaderboard FOR UPDATE USING (auth.uid() = player_id);

CREATE POLICY "Users can read own quests" ON daily_quests FOR SELECT USING (auth.uid() = player_id);
CREATE POLICY "Users can insert own quests" ON daily_quests FOR INSERT WITH CHECK (auth.uid() = player_id);
CREATE POLICY "Users can update own quests" ON daily_quests FOR UPDATE USING (auth.uid() = player_id);

-- Index for leaderboard sorting
CREATE INDEX IF NOT EXISTS idx_leaderboard_score ON leaderboard(score DESC);
