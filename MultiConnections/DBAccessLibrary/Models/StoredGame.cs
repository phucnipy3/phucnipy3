﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DBAccessLibary.Models
{
    public class StoredGame
    {
        public int Id { get; set; }
        public StoredPlayer Winner { get; set; }
        public int Order { get; set; }
        public List<Move> Moves { get; set; } = new List<Move>();

    }
}