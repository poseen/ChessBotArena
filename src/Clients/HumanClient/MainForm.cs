﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BoardGame.Game.Chess;
using BoardGame.Game.Chess.Moves;
using BoardGame.Model.Api.ChessGamesControllerModels;
using BoardGame.ServiceClient;
using BoardGame.Tools.Common;
//using Easy.Common.Extensions;

namespace BoardGame.HumanClient
{
    public partial class MainForm : Form
    {
        private ChessMechanism _mechanism;
        private ChessRepresentation _game;
        private Guid? _selectedMatchId;
        private ChessServiceClientSession _client;
        private DateTime _lastUpdate = DateTime.MinValue;
        private int _countDown = 10;

#if DEBUG
        private readonly string _baseUrl = "http://localhost/BoardGame.Service";
#else
        private readonly string _baseUrl = "http://poseen-001-site1.gtempurl.com";
#endif

        public MainForm()
        {
            InitializeComponent();
            tabPageGame.Tag = Tabs.GamePage;
            tabPageMatches.Tag = Tabs.MatchesPage;
            tabPagePlayers.Tag = Tabs.PlayersPage;
            _client = new ChessServiceClientSession(_baseUrl);
        }

        private async void tabMain_Selected(object sender, TabControlEventArgs e)
        {
            var page = (Tabs?) e.TabPage.Tag;

            panelGame.Visible = page == Tabs.GamePage;
            panelPlayers.Visible = page == Tabs.PlayersPage;
            panelMatches.Visible = page == Tabs.MatchesPage;

            await RefreshAll();
        }

        private async Task RefreshPlayers()
        {
            if (await CheckSessionValidity() == false)
            {
                return;
            }

            var players = await _client.GetPlayers();
            if (players == null)
            {
                tabPagePlayers.Enabled = true;
                MessageBox.Show("Couldn't get list of players.");
                return;
            }

            var newList = players.Select(x => x.Name).OrderBy(x => x).AsEnumerable();
            var oldList = listViewPlayers.Items.OfType<ListViewItem>().Select(x => x.Text).OrderBy(x => x)
                .AsEnumerable();

            if (RandomOrderedSequelEquals(oldList, newList))
            {
                return;
            }

            tabPagePlayers.Enabled = false;
            listViewPlayers.Items.Clear();

            foreach (var player in players)
            {
                listViewPlayers.Items.Add(new ListViewItem()
                {
                    ImageKey = player.IsBot ? "Robot" : "Brain",
                    Text = player.Name,
                });
            }

            tabPagePlayers.Enabled = true;
        }

        private static bool RandomOrderedSequelEquals<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            var firstSet = first?.ToHashSet() ?? new HashSet<T>();
            var secondSet = second?.ToHashSet() ?? new HashSet<T>();
            return firstSet.SetEquals(secondSet);
        }

        private async Task RefreshMatches()
        {
            if (await CheckSessionValidity() == false)
            {
                return;
            }

            var matches = await _client.GetMatches();
            if (matches == null)
            {
                tabPageMatches.Enabled = true;
                MessageBox.Show("Couldn't get list of matches.");
                return;
            }

            var oldList = listViewMatches.Items.OfType<ListViewItem>()
                .Select(x => x.Tag as ChessGameDetails)
                .Where(x => x != null)
                .Select(x => x.Id)
                .OrderBy(x => x)
                .AsEnumerable();

            var newList = matches.Select(x => x.Id).OrderBy(x => x).AsEnumerable();

            if (RandomOrderedSequelEquals(oldList, newList))
            {
                return;
            }

            tabPageMatches.Enabled = false;
            listViewMatches.Items.Clear();

            foreach (var match in matches)
            {
                string imageKey;
                switch (match.Outcome)
                {
                    case GameState.InProgress:
                        imageKey = "InProgress";
                        break;

                    case GameState.WhiteWon:
                        imageKey = "WhiteWon";
                        break;

                    case GameState.BlackWon:
                        imageKey = "BlackWon";
                        break;

                    case GameState.Draw:
                        imageKey = "Draw";
                        break;

                    default:
                        imageKey = string.Empty;
                        break;
                }

                var details = await _client.GetMatch(match.Id.ToString());

                listViewMatches.Items.Add(new ListViewItem()
                {
                    ImageKey = imageKey,
                    Text = match.Name,
                    Tag = details
                });
            }

            tabPageMatches.Enabled = true;
        }

        private async Task RefreshGame(bool force = false)
        {
            if (await CheckSessionValidity() == false || !_selectedMatchId.HasValue)
            {
                return;
            }

            var details = await _client.GetMatch(_selectedMatchId.Value.ToString());
            if (details == null)
            {
                MessageBox.Show("Couldn't load match details.");
                chessBoardGamePanel1.Enabled = true;
                tabPageGame.Enabled = true;
                return;
            }

            if (!force)
            {
                var newList = details.Representation.History.Select(x => x.ToString());
                var oldList = listboxMoves.Items.OfType<string>();

                if (RandomOrderedSequelEquals(newList, oldList))
                {
                    tabPageGame.Enabled = true;
                    chessBoardGamePanel1.Enabled = true;
                    return;
                }
            }

            tabPageGame.Enabled = false;
            chessBoardGamePanel1.Enabled = false;

            listboxMoves.Items.Clear();

            var game = new ChessRepresentationInitializer().Create();

            foreach (var t in details.Representation.History)
            {
                game = _mechanism.ApplyMove(game, t);
                listboxMoves.Items.Add(t.ToString());
            }

            chessBoardGamePanel1.ChessRepresentation = game;

            var gameState = _mechanism.GetGameState(game);
            labelGameState.Text = GameStateToString(gameState, game.CurrentPlayer);

            var isItMyTurn = IsItMyTurn(details);

            if (!isItMyTurn)
            {
                btnAcceptDraw.Enabled = false;
                btnDeclineDraw.Enabled = false;
                btnOfferDraw.Enabled = false;
                btnResign.Enabled = false;
                tabPageGame.Enabled = true;
                chessBoardGamePanel1.Enabled = true;
                return;
            }

            var myColor = MyColorInGame(details);
            var possibleSpecialMoves = _mechanism.GenerateMoves(game).Where(x => x.Owner == myColor).OfType<SpecialMove>().ToList();

            btnAcceptDraw.Enabled = possibleSpecialMoves.Any(x => x.Message == MessageType.DrawAccept);
            btnDeclineDraw.Enabled = possibleSpecialMoves.Any(x => x.Message == MessageType.DrawDecline);
            btnOfferDraw.Enabled = possibleSpecialMoves.Any(x => x.Message == MessageType.DrawOffer);
            btnResign.Enabled = possibleSpecialMoves.Any(x => x.Message == MessageType.Resign);

            tabPageGame.Enabled = true;
        }

        private static string GameStateToString(GameState state, ChessPlayer? nextPlayer = null)
        {
            switch (state)
            {
                case GameState.InProgress:
                    switch (nextPlayer)
                    {
                        case ChessPlayer.White: return "Next: White";
                        case ChessPlayer.Black: return "Next: Black";
                        default: return "In progress";
                    }
                case GameState.WhiteWon: return "White won";
                case GameState.BlackWon: return "Black won";
                case GameState.Draw: return "Draw";
                default: throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private async void buttonLogin_Click(object sender, EventArgs e)
        {
            await Login();
        }

        private async Task ToggleLoginControls()
        {
            _game = new ChessRepresentationInitializer().Create();
            _mechanism = new ChessMechanism();
            var isSessionAlive = await CheckSessionValidity();

            panelLogin.Visible = !isSessionAlive;
            panelLogout.Visible = isSessionAlive;
        }

        private async Task Login()
        {
            var success = await _client.Login(textboxUsername.Text, textboxPassword.Text);

            if (!success)
            {
                MessageBox.Show("Login failed!", "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                await ToggleLoginControls();
                timerRefresh.Enabled = false;
            }
            else
            {
                labelLoginStatus.Text = $"{_client.LoginInformation.Username} logged in.";
                await RefreshAll();
                await ToggleLoginControls();
                timerRefresh.Enabled = true;
            }
        }

        private async void btnLogout_Click(object sender, EventArgs e)
        {
            timerRefresh.Enabled = false;
            _client.Logout();
            await ToggleLoginControls();
        }

        private void MainForm_Move(object sender, EventArgs e)
        {
            chessBoardGamePanel1.Refresh();
        }

        private async Task<bool> CheckSessionValidity()
        {
            if (_client == null)
            {
                return false;
            }

            return await _client.EnsureSessionIsActive();
        }

        private async void btnChallenge_Click(object sender, EventArgs e)
        {
            if (await CheckSessionValidity() == false)
            {
                return;
            }

            if (listViewPlayers.SelectedItems.Count == 0)
            {
                return;
            }

            var selectedPlayer = listViewPlayers.SelectedItems[0].Text;

            var newGame = await _client.ChallengePlayer(selectedPlayer);

            if (newGame == null)
            {
                MessageBox.Show("Couldn't send challenge.");
                return;
            }

            _selectedMatchId = newGame.Id;
            tabPageMatches.Select();
        }

        private async Task RefreshAll(bool force = false)
        {
            if (!force && DateTime.Now - _lastUpdate < TimeSpan.FromSeconds(10))
            {
                return;
            }

            await RefreshMatches();
            await RefreshPlayers();
            await RefreshGame();

            _lastUpdate = DateTime.Now;
        }

        private async void listViewMatches_ItemActivate(object sender, EventArgs e)
        {
            var listView = (sender as ListView);

            // ReSharper disable once PossibleNullReferenceException
            var item = (listView?.Items.Count ?? 0) > 0 ? listView.SelectedItems[0] : null;

            var details = (ChessGameDetails) item?.Tag;
            var representation = details?.Representation;

            labelMatchPreviewStatus.Text = string.Empty;

            if (representation != null)
            {
                chessBoardPreview.ChessRepresentation = representation;
                var state = _mechanism.GetGameState(representation);
                if (state == GameState.InProgress)
                {
                    var sb = new StringBuilder();

                    sb.Append($"Next: {representation.CurrentPlayer}");
                    switch (representation.CurrentPlayer)
                    {
                        case ChessPlayer.White:
                            if (details.WhitePlayer.UserName == _client.LoginInformation.Username)
                            {
                                sb.Append(" (You)");
                            }
                            break;
                        case ChessPlayer.Black:
                            if (details.WhitePlayer.UserName == _client.LoginInformation.Username)
                            {
                                sb.Append(" (You)");
                            }
                            break;

                        default:
                            break;
                    }

                    labelMatchPreviewStatus.Text = sb.ToString();
                }
                else
                {
                    labelMatchPreviewStatus.Text = state.ToString();
                }
            }
            _selectedMatchId = details?.Id;

            if (_selectedMatchId == null)
            {
                return;
            }

            await RefreshGame();
            //tabMain.SelectedTab = tabPageGame;
        }

        private void listViewMatches_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            listViewMatches_ItemActivate(sender, e);

            if (_selectedMatchId == null)
            {
                return;
            }

            tabPageGame.Show();
        }

        private bool IsItMyTurn(ChessGameDetails details)
        {
            var representation = details.Representation;
            var state = _mechanism.GetGameState(details.Representation);

            if (state != GameState.InProgress)
            {
                return false;
            }

            var sb = new StringBuilder();

            switch (representation.CurrentPlayer)
            {
                case ChessPlayer.White:
                    if (details.WhitePlayer.UserName == _client.LoginInformation.Username)
                    {
                        return true;
                    }
                    break;

                case ChessPlayer.Black:
                    if (details.WhitePlayer.UserName == _client.LoginInformation.Username)
                    {
                        return true;
                    }
                    break;
            }

            labelMatchPreviewStatus.Text = sb.ToString();

            return false;
        }

        private ChessPlayer MyColorInGame(ChessGameDetails details)
        {
            var representation = details.Representation;

            switch (representation.CurrentPlayer)
            {
                case ChessPlayer.White:
                    if (details.WhitePlayer.UserName == _client.LoginInformation.Username)
                    {
                        return ChessPlayer.White;
                    }
                    else
                    {
                        return ChessPlayer.Black;
                    }

                case ChessPlayer.Black:
                    if (details.WhitePlayer.UserName == _client.LoginInformation.Username)
                    {
                        return ChessPlayer.Black;
                    }
                    else
                    {
                        return ChessPlayer.White;
                    }
            }

            throw new ArgumentOutOfRangeException();
        }

        private async void chessBoardGamePanel1_OnValidMoveSelected(object source, ChessboardMoveSelectedEventArg eventArg)
        {
            if (await CheckSessionValidity() == false || !_selectedMatchId.HasValue)
            {
                return;
            }

            var result = await _client.SendMove(_selectedMatchId.Value, eventArg.Move);
            await RefreshGame(true);
            if (!result)
            {
                MessageBox.Show("Couldn't send in move.");
            }
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await RefreshAll(true);
        }

        private async void timerRefresh_Tick(object sender, EventArgs e)
        {
            _countDown--;
            btnSync.Text = $"Sync now (auto:{_countDown}s)";
            if (_countDown <= 0)
            {
                btnSync.Text = $"Syncing...";
                await RefreshAll();
                _countDown = 10;
            }
        }

        private async void tabLadder_Enter(object sender, EventArgs e)
        {
            await RefreshLadder();
        }

        private async Task RefreshLadder()
        {
            bool? parameter = null;
            if (checkboxShowBots.Checked && !checkboxShowHumans.Checked)
            {
                parameter = true;
            }
            else if (!checkboxShowBots.Checked && checkboxShowHumans.Checked)
            {
                parameter = false;
            }

            var result = await _client.GetLadder(parameter);

            if (result == null)
            {
                MessageBox.Show("Couldn't get ladder.");
                return;
            }

            listviewLadder.Items.Clear();

            foreach (var ladderItem in result.OrderBy(x => x.Place))
            {
                listviewLadder.Items.Add(new ListViewItem()
                {
                    Text = $"{ladderItem.Place}",
                    ImageKey = ladderItem.IsBot ? "Robot" : "Brain",
                    SubItems =
                    {
                        ladderItem.Name,
                        $"{ladderItem.Points:F}"
                    }
                });
            }
        }

        private async void checkboxShowHumans_CheckedChanged(object sender, EventArgs e)
        {
            await RefreshLadder();
        }

        private async void checkboxShowBots_CheckedChanged(object sender, EventArgs e)
        {
            await RefreshLadder();
        }
    }
}