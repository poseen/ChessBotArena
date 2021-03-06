﻿using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using BoardGame.Game.Chess;
using BoardGame.Game.Chess.Moves;
using BoardGame.Model.Api.ChessGamesControllerModels;
using BoardGame.ServiceClient;

namespace BoardGame.Tools.ChessServiceTestApp
{
    public partial class MainForm : Form
    {
        private readonly ChessServiceClient _client;

        private string _jwtToken;

#if DEBUG
        private readonly string _baseUrl = "http://localhost/BoardGame.Service";
#else
        private readonly string _baseUrl = "http://poseen-001-site1.gtempurl.com";
#endif

        public MainForm()
        {
            InitializeComponent();
            _client = new ChessServiceClient(_baseUrl);
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            labelStatus.Text = "Connecting to server...";
            string result;
            tableLayoutMain.Enabled = false;
            try
            {
                result = await _client.GetVersionAsync();
            }
            catch (Exception)
            {
                MessageBox.Show("Service seems down");
                return;
            }

            Text = $"Chess Client Tester App (Server version: v{result})";
            tableLayoutMain.Enabled = true;
            labelStatus.Text = string.Empty;
        }

        private async void buttonLogin_Click(object sender, EventArgs e)
        {
            labelStatus.Text = "Logging in...";
            LoginResult result;

            try
            {
                var username = textboxUsername.Text;
                var password = textboxPassword.Text;
                result = await _client.LoginAsync(username, password);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Service seems down.");
                labelStatus.Text = string.Empty;
                return;
            }

            if (result == null)
            {
                labelLoginStatus.Text = "Unauthorized";
                _jwtToken = null;
                labelStatus.Text = string.Empty;
                return;
            }

            _jwtToken = result.TokenString;

            labelLoginStatus.Text = result.Username;
            labelStatus.Text = string.Empty;

            await RefreshLists();
        }

        private async void buttonRefresh_Click(object sender, EventArgs e)
        {
            await RefreshLists();
        }

        private async Task RefreshLists()
        {
            labelStatus.Text = "Getting list of matches...";
            var matches = await _client.GetMatchesAsync(_jwtToken);
            listboxMatches.Items.Clear();

            if (matches == null)
            {
                MessageBox.Show("Couldn't get list of matches. (Maybe unauthorized?)");
                labelStatus.Text = "Error while getting list of matches.";
                return;
            }

            foreach (var match in matches)
            {
                listboxMatches.Items.Add(new DecoratedMatch
                {
                    Name = match.Name,
                    Outcome = match.Outcome,
                    Opponent = match.Opponent,
                    BlackPlayer = match.BlackPlayer,
                    ChallengeDate = match.ChallengeDate,
                    Id = match.Id,
                    InitiatedBy = match.InitiatedBy,
                    LastMoveDate = match.LastMoveDate,
                    WhitePlayer = match.WhitePlayer
                });
            }
        }

        private async void listboxMatches_SelectedIndexChanged(object sender, EventArgs e)
        {
            var match = (DecoratedMatch)listboxMatches.SelectedItem;
            var mechanism = new ChessMechanism();
            var board = new ChessRepresentationInitializer().Create();

            if (match == null)
            {
                return;
            }

            labelStatus.Text = $"Getting details of selected match... ({match.Name})";
            var details = await _client.GetMatchAsync(_jwtToken, match.Id.ToString());
            listboxGameHistory.Items.Clear();

            if (details?.Representation?.History == null)
            {
                MessageBox.Show("Couldn't get details of selected match. (Maybe unauthorized?)");
                labelStatus.Text = "Error while getting details of match";
                return;
            }

            listboxGameHistory.Items.Add(new ChessRepresentationStage()
            {
                AfterMove = null,
                ChessRepresentation = board
            });

            foreach (var move in details.Representation.History)
            {
                board = mechanism.ApplyMove(board, move);

                var item = new ChessRepresentationStage
                {
                    AfterMove = move,
                    ChessRepresentation = board
                };

                listboxGameHistory.Items.Add(item);
            }
        }

        private void listboxGameHistory_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (!(listboxGameHistory.SelectedItem is ChessRepresentationStage item))
            {
                return;
            }

            chessBoardVisualizerPictureBox1.ChessRepresentation = item.ChessRepresentation;
        }
    }

    internal class ChessRepresentationStage
    {
        public BaseMove AfterMove { get; set; }

        public ChessRepresentation ChessRepresentation { get; set; }

        public override string ToString()
        {
            return AfterMove?.ToString() ?? "Init";
        }
    }

    internal class DecoratedMatch : ChessGame
    {
        public override string ToString()
        {
            return $"{Name}({Outcome})";
        }
    }
}
