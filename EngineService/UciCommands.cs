﻿namespace EngineService
{
    public class UciCommands
    {
        // requests to the engine
        public const string ENG_UCI = "uci";
        public const string ENG_ISREADY = "isready";
        public const string ENG_UCI_NEW_GAME = "ucinewgame";
        public const string ENG_POSITION = "position";
        public const string ENG_POSITION_STARTPOS = "position startpos moves";
        public const string ENG_GO = "go";
        public const string ENG_GO_MOVE_TIME = "go movetime";
        public const string ENG_GO_INFINITE = "go infinite";
        public const string ENG_STOP = "stop";
        public const string ENG_SET_MULTIPV = "setoption name multipv value";

        // responses from the engine
        public const string ENG_UCI_OK = "uciok";
        public const string ENG_READY_OK = "readyok";
        public const string ENG_BEST_MOVE = "bestmove";

        // prefix in the engine's message naming itself
        public const string ENG_ID_NAME = "id name";
    }
}

