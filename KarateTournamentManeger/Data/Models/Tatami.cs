﻿using KarateTournamentManager.Controllers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static KarateTournamentManager.Constants.ModelConstants;

namespace KarateTournamentManager.Data.Models
{
    public class Tatami
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [StringLength(MaxTatamiLenght)]
        public int Number { get; set; }
        public Guid? TimerManagerId { get; set; }

        [ForeignKey(nameof(TimerManagerId))]
        public Participant? TimerManager { get; set; }
        
        public Guid TournamentId { get; set; }

        [ForeignKey(nameof(TournamentId))]
        public Tournament Tournament { get; set; } = null!;
    }
}