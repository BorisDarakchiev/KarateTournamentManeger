﻿namespace KarateTournamentManager.Models.ViewModels
{
    using global::KarateTournamentManager.Enums;
    using KarateTournamentManager.Controllers;
    using System;
    using System.ComponentModel.DataAnnotations;

    public class MatchViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Участник 1")]
        public string Participant1Name { get; set; } = string.Empty;
        public Guid Participant1Id { get; set; }

        [Display(Name = "Участник 2")]
        public string Participant2Name { get; set; } = string.Empty;

        public Guid Participant2Id { get; set; }

        [Display(Name = "Резултат на участник 1")]
        public int Participant1Score { get; set; }

        [Display(Name = "Резултат на участник 2")]
        public int Participant2Score { get; set; }

        [Display(Name = "Част")]
        public MatchPeriod Period { get; set; }

        [Display(Name = "Татами")]
        public int Tatami { get; set; }

        public Timer? Timer { get; set; }

        [Display(Name = "Статус")]
        public MatchStatus Status { get; set; }

        [Display(Name = "Турнир")]
        public Guid TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public string? WinnerName { get; set; }
    }
}
