﻿using KarateTournamentManager.Controllers;
using KarateTournamentManager.Data;
using KarateTournamentManager.Data.Models;
using KarateTournamentManager.Enums;
using KarateTournamentManager.Identity;
using KarateTournamentManager.Models;
using KarateTournamentManager.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static KarateTournamentManager.Constants.ModelConstants;



namespace KarateTournamentManager.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;

        public TournamentService(ApplicationDbContext _context, UserManager<ApplicationUser> _userManager)
        {
            context = _context;
            userManager = _userManager;
        }

        public async Task<List<TournamentViewModel>> GetTournamentsViewModelsAsync(string userId)
        {
            var tournaments = await context.Tournaments
                .Include(t => t.EnrolledParticipants)
                .Where(t => t.Date >= DateTime.Now)
                .OrderBy(t => t.Date)
                .ToListAsync();

            var currentUser = userId != null
                ? await userManager.Users.Include(u => u.Participant).FirstOrDefaultAsync(u => u.Id == userId)
                : null;
            var currentUserParticipantId = currentUser?.Participant?.Id;

            return tournaments.Select(t => new TournamentViewModel
            {
                Id = t.Id,
                Location = t.Location,
                Description = t.Description,
                Date = t.Date,
                Status = t.Status,
                EnrolledParticipantsCount = t.EnrolledParticipants?.Count ?? 0,
                EnrolledParticipants = t.EnrolledParticipants != null
                    ? t.EnrolledParticipants
                        .Select(p => new ParticipantViewModel { Name = p.Name, Id = p.Id })
                        .ToList()
                    : new List<ParticipantViewModel>(),
                IsParticipant = currentUserParticipantId.HasValue &&
                                t.EnrolledParticipants.Any(p => p.Id == currentUserParticipantId.Value)
            }).ToList();
        }

        public async Task<List<TournamentViewModel>> GetTournamentsAsync()
        {
            return await context.Tournaments
                .OrderByDescending(t => t.Date)
                .Select(t => new TournamentViewModel
                {
                    Id = t.Id,
                    Location = t.Location,
                    Description = t.Description,
                    Date = t.Date,
                    Status = t.Status,
                    EnrolledParticipantsCount = t.EnrolledParticipants.Count
                })
                .AsNoTracking()
                .ToListAsync();
        }
        public Task<TournamentViewModel> CreateTournamentViewModelAsync()
        {
            var model = new TournamentViewModel
            {
                Date = DateTime.Now,
                Status = TournamentStatus.Upcoming,
                EnrolledParticipants = new List<ParticipantViewModel>(),
                Stages = new List<StageViewModel>(),
            };

            return Task.FromResult(model);
        }

        public async Task<bool> CreateTournamentAsync(TournamentViewModel model)
        {
            var tournament = new Tournament
            {
                Id = Guid.NewGuid(),
                Location = model.Location,
                Description = model.Description,
                Date = model.Date,
                Status = TournamentStatus.Upcoming,
                EnrolledParticipants = new List<Participant>(),
            };

            context.Tournaments.Add(tournament);
            await context.SaveChangesAsync();

            return true;
        }

        public async Task<TournamentViewModel> GetTournamentDetailsAsync(Guid id)
        {
            var tournament = await context.Tournaments
                .Include(t => t.EnrolledParticipants)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null)
            {
                return null!;
            }

            var enrolledParticipants = tournament.EnrolledParticipants?.Select(p =>
            {
                var user = context.Users.FirstOrDefault(u => u.ParticipantId == p.Id);
                return new ParticipantViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Email = user?.Email ?? "N/A"
                };
            }).ToList();

            var matches = await context.Matchеs
                .Include(m => m.Stage)
                .Where(m => m.Stage.TournamentId == id)
                .Include(m => m.Participant1)
                .Include(m => m.Participant2)
                .ToListAsync();

            var stagesWithMatches = matches
                .GroupBy(m => m.Stage)
                .Select(group => new StageViewModel
                {
                    Id = group.Key.Id,
                    Name = group.Key.Name,
                    Matches = group.Select(m => new MatchViewModel
                    {
                        Participant1Name = m.Participant1 != null ? m.Participant1.Name : "Не е определен",
                        Participant2Name = m.Participant2 != null ? m.Participant2.Name : "Не е определен",
                        Participant1Score = m.Participant1Score,
                        Participant2Score = m.Participant2Score,
                        Period = m.Period,
                        Tatami = m.Tatami,
                        RemainingTime = m.RemainingTime.ToString(@"hh\:mm\:ss"),
                        Status = m.Status
                    }).ToList()
                }).ToList();

            var sortedStages = new List<StageViewModel>();
            int indexInsert = 0;

            foreach (StageOrder stageOrder in Enum.GetValues(typeof(StageOrder)))
            {
                var stage = stagesWithMatches.FirstOrDefault(s => s.Name == StageToName[stageOrder.ToString()]);

                if (stage != null)
                {
                    if (stageOrder == StageOrder.Preliminary)
                    {
                        sortedStages.Insert(0, stage);
                        indexInsert = 1;
                    }
                    else
                    {
                        sortedStages.Insert(indexInsert, stage);
                    }
                }
            }

            return new TournamentViewModel
            {
                Id = tournament.Id,
                Location = tournament.Location,
                Description = tournament.Description,
                Date = tournament.Date,
                Status = tournament.Status,
                EnrolledParticipantsCount = enrolledParticipants.Count,
                EnrolledParticipants = enrolledParticipants,
                Stages = sortedStages
            };
        }

        public async Task<bool> DeleteTournamentAsync(Guid id)
        {
            var tournament = await context.Tournaments.FindAsync(id);
            if (tournament == null)
            {
                return false;
            }

            context.Tournaments.Remove(tournament);
            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveParticipantAsync(Guid tournamentId, Guid participantId)
        {
            var tournament = await context.Tournaments
                .Include(t => t.EnrolledParticipants)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
            {
                return false;
            }

            var participant = tournament.EnrolledParticipants?.FirstOrDefault(p => p.Id == participantId);
            if (participant == null)
            {
                return false;
            }

            tournament.EnrolledParticipants?.Remove(participant);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<string?> FinalizeEnrollmentAsync(Guid tournamentId)
        {
            var tournament = await context.Tournaments
                .Include(t => t.EnrolledParticipants)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
            {
                return "Турнирът не е намерен.";

            }

            if (tournament.Status != TournamentStatus.Upcoming)
            {
                return "Не може да се изпълните това действие, когато турнира е започнал или завършил!";
            }

            var participants = tournament.EnrolledParticipants.ToList();

            if (participants.Count < 2)
            {
                return "Необходими са поне двама участници за създаване на етапи.";
            }

            await RemoveExistingStagesAsync(tournamentId);
            await CreateStagesAsync(tournament);
            tournament.Status = TournamentStatus.Ongoing;
            await context.SaveChangesAsync();

            return string.Empty;
        }

        private async Task RemoveExistingStagesAsync(Guid tournamentId)
        {
            var existingStages = await context.Stages
                .Where(s => s.TournamentId == tournamentId)
                .ToListAsync();

            foreach (var stage in existingStages)
            {
                var matches = await context.Matchеs
                    .Where(m => m.StageId == stage.Id)
                    .ToListAsync();

                context.Matchеs.RemoveRange(matches);
                context.Stages.Remove(stage);
            }

            await context.SaveChangesAsync();
        }


        public async Task CreateStagesAsync(Tournament tournament)
        {
            int participants = tournament.EnrolledParticipants.Count;
            int powerOfTwo = (int)Math.Pow(2, Math.Floor(Math.Log2(participants)));
            int preliminaryMatches = participants - powerOfTwo;
            int participantsInPreliminary = 2 * preliminaryMatches;
            if (preliminaryMatches > 0)
            {
                Stage stage = new Stage() { TournamentId = tournament.Id, Name = StageToName["Preliminary"], StageOrder = StageOrder.Preliminary };
                var matches = new List<Match>();
                while (preliminaryMatches != 0)
                {
                    var match = new Match() { StageId = stage.Id };
                    matches.Add(match);
                    preliminaryMatches--;
                }
                await context.Matchеs.AddRangeAsync(matches);
                await context.Stages.AddAsync(stage);
                await context.SaveChangesAsync();
            }
            if (participants == 3)
            {
                Stage stage = new Stage() { TournamentId = tournament.Id, Name = StageToName["RoundRobin"], StageOrder = StageOrder.RoundRobin };
                var matches = new List<Match>
                {
                    new Match(){ StageId = stage.Id },
                    new Match(){ StageId = stage.Id },
                    new Match(){ StageId = stage.Id }
                };
                await context.Matchеs.AddRangeAsync(matches);
                await context.Stages.AddAsync(stage);
                await context.SaveChangesAsync();
            }
            if (participants >= 4 || participants == 2)
            {
                int numberStages = (int)Math.Log2(powerOfTwo);
                var allStages = new List<Stage>();
                var allMatches = new List<Match>();

                for (int i = 1; i <= numberStages; i++)
                {
                    int indexOfFinal = (int)StageOrder.Final;
                    int indexOfCurrentStage = i + indexOfFinal - 1;
                    StageOrder currentStage = (StageOrder)Enum.GetValues(typeof(StageOrder)).GetValue(indexOfCurrentStage);

                    Stage stage = new Stage() { TournamentId = tournament.Id, Name = StageToName[currentStage.ToString()], StageOrder = currentStage };
                    int numberOfMatches = (int)Math.Pow(2, i - 1);

                    while (numberOfMatches != 0)
                    {
                        var match = new Match() { StageId = stage.Id };
                        allMatches.Add(match);
                        numberOfMatches--;
                    }
                    allStages.Add(stage);
                }
                await context.Stages.AddRangeAsync(allStages);
                await context.Matchеs.AddRangeAsync(allMatches);
                await context.SaveChangesAsync();
            }
        }
    }
}



