using System;

namespace Chess2Eyal
{
    class Program
    {
        static void Main(string[] args)
        {
            Board board = new Board();
            string turn = "W";
            string nextTurn = "B";
            for (; ; )
            {
                if (board.getCheck())
                    Console.WriteLine((turn == "W") ? "White is under check!" : "Black is under check!");
                board.printBoard();
                board.addHistory();
                if (board.checkHistoryRepetition())
                {
                    Console.WriteLine("3 fold repetition");
                    return;
                }
                if (board.insufficientMaterial())
                {
                    Console.WriteLine("Tie due insufficient material");
                    return;
                }
                do
                {
                    board.adjustBoard();
                    Console.WriteLine((turn == "W") ? "White's turn" : "Black's turn");
                    board.getMove();
                } while (!board.getMovingPiece().getName().StartsWith(turn) || !board.checkCurrentMove(true));

                board.movePiece();
                board.setCheck(board.lookForChecks(nextTurn));
                if (board.getCheck())
                {
                    if (!board.legalMovesExist(nextTurn))
                    {
                        board.printBoard();
                        Console.WriteLine((nextTurn == "W") ? "White is mated!" : "Black is mated!");
                        Console.WriteLine("Press any key to close window");
                        Console.ReadLine();
                        return;
                    }
                }
                else //no check 
                {
                    if (!board.legalMovesExist(nextTurn))
                    {
                        Console.WriteLine("No moves... it's a stalemate");
                        board.printBoard();
                        return;
                    }
                }
                board.changeIsWhiteTurn();
                turn = ((board.getIsWhiteTurn()) ? "W" : "B");
                nextTurn = ((board.getIsWhiteTurn()) ? "B" : "W");
                if (board.getMovingPiece().getName().EndsWith("P"))
                {
                    board.setNumberOf50Moves(0);
                }
                else if (!board.sumPiecesChanged())
                {
                    board.setNumberOf50Moves(board.getNumberOf50Moves() + 1);
                }
                if (board.getNumberOf50Moves() == 49)//should be 99 move = black + white and start 0
                {
                    Console.WriteLine("Tie due to 50 moves rule");
                    return;
                }

                
            }
        }
    }

    class Board
    {
        string history;
        Piece[,] gBoard = new Piece[8, 8];
        int[] gMoveDestination;
        int[] gMoveFrom;
        bool gWhiteCastled;
        bool gBlackCastled;
        string gEnPassantTurn;
        bool gIsEnPassant;
        int[] gWEnPassantPosition = new int[2]; //from where to where... 1/2 possibilities
        int[] gBEnPassantPosition = new int[2]; //two are needed because on consecutive moves both sides can create enPassant opportunity
        bool gIsWhiteTurn = true;
        Piece gWK; Piece gBK;
        Piece gMovingPiece;
        bool gCheck;
        static int gMoves50 = 0;
        public static int gSumPiecesValues;//public for debugging only - getSumPieces()


        public Board(string copy) { }
        public Board()
        {
            for (int column = 0; column < gBoard.GetLength(1); column++)//gBoard.GetLength(1)
            {
                gBoard[1, column] = new Pawn("WP", new int[] { 1, column });
                gBoard[6, column] = new Pawn("BP", new int[] { 6, column });
            }

            gBoard[0, 0] = new Rook("WR", new int[] { 0, 0 });
            gBoard[0, 7] = new Rook("WR", new int[] { 0, 7 });
            gBoard[7, 7] = new Rook("BR", new int[] { 7, 7 });
            gBoard[7, 0] = new Rook("BR", new int[] { 7, 0 });
            gBoard[0, 1] = new Knight("WN", new int[] { 0, 1 });
            gBoard[0, 6] = new Knight("WN", new int[] { 0, 6 });
            gBoard[7, 1] = new Knight("BN", new int[] { 7, 1 });
            gBoard[7, 6] = new Knight("BN", new int[] { 7, 6 });
            gBoard[0, 2] = new Bishop("WB", new int[] { 0, 2 });
            gBoard[0, 5] = new Bishop("WB", new int[] { 0, 5 });
            gBoard[7, 2] = new Bishop("BB", new int[] { 7, 2 });
            gBoard[7, 5] = new Bishop("BB", new int[] { 7, 5 });
            gBoard[0, 3] = new Queen("WQ", new int[] { 0, 3 });
            gBoard[7, 3] = new Queen("BQ", new int[] { 7, 3 });
            gWK = gBoard[0, 4] = new King("WK", new int[] { 0, 4 });
            gBK = gBoard[7, 4] = new King("BK", new int[] { 7, 4 });
        }

        public int[] getDestination()
        {
            return gMoveDestination;
        }
        public int[] getCurrentSquare()
        {
            return gMoveFrom;
        }
        public bool checkHistoryRepetition()
        {
            string state = registerBoardPosition();
            string[] states = history.Split("start");
            string[] stateCheck = state.Split("start");
            int counter = 0;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].Trim() == stateCheck[0].Trim())
                    counter++;
            }
            if (counter >= 3)
                return true; //history repeats itself
            return false;
        }
        public void addHistory()
        {
            string state = registerBoardPosition();
            history = history + "start " + state;
        }

        public string registerBoardPosition()
        {
            string state = "";
            for (int i = 0; i < gBoard.GetLength(0); i++)
            {
                for (int j = 0; j < gBoard.GetLength(1); j++)
                {
                    if (gBoard[i, j] != null)
                    {
                        state += gBoard[i, j].getName();
                        state += i;
                        state += j;
                    }
                }
            }
            return state;
        }
        public int getNumberOf50Moves()
        {
            return gMoves50;
        }

        public void setNumberOf50Moves(int gmoves50plus1)
        {
            gMoves50 = gmoves50plus1;
        }

        public bool insufficientMaterial()//true when insufficient
        {
            int minorPieceWhite = 0;
            int minorPieceBlack = 0;
            if (gSumPiecesValues > 6) //2 minors = 6
                return false;
            for (int i = 0; i < gBoard.GetLength(0); i++)
            {
                for (int j = 0; j < gBoard.GetLength(1); j++)
                {
                    if (gBoard[i, j] != null)
                    {
                        if (gBoard[i, j].getName().EndsWith("P"))
                        {
                            return false;
                        }
                        if (gBoard[i, j].getValue() == 3)
                        {
                            if (gBoard[i, j].getName().StartsWith("W"))
                                minorPieceWhite++;
                            else
                                minorPieceBlack++;
                        }
                        if (minorPieceWhite == 2 || minorPieceBlack == 2)// two of the same color
                        {
                            return false;
                        }
                        if (gSumPiecesValues > 4) //1 rook
                            return false;
                    }
                }
            }
            return true;
        }
        public bool sumPiecesChanged()
        {
            int sum = 0;
            for (int i = 0; i < gBoard.GetLength(0); i++)
            {
                for (int j = 0; j < gBoard.GetLength(1); j++)
                {
                    if (gBoard[i, j] != null)
                    {
                        sum += gBoard[i, j].getValue();
                    }
                }
            }
            if (sum != gSumPiecesValues)
            {
                gSumPiecesValues = sum;
                gMoves50 = 0;
            }
            else
            {
                return false;
            }
            return true;
        }
        public bool checkEnpassant()
        {
            return gIsEnPassant;
        }

        public void changeEnpassant()
        {
            gIsEnPassant = !gIsEnPassant;
        }

        public string checkEnpassantTurn()
        {
            return gEnPassantTurn;
        }

        public Piece getMovingPiece()
        {
            return gMovingPiece;
        }
        public void movePiece() //role : move pieces and no more!
        {
            int colDifference = gMoveDestination[1] - gMoveFrom[1];
            if (gIsEnPassant && gMoveDestination[0] == ((gIsWhiteTurn) ? gWEnPassantPosition[0] : gBEnPassantPosition[0])
                && gMoveDestination[1] == ((gIsWhiteTurn) ? gWEnPassantPosition[1] : gBEnPassantPosition[1]))
            {
                gMovingPiece.setPosition(gMoveDestination);
                gBoard[gMoveDestination[0], gMoveDestination[1]] = gMovingPiece;
                gBoard[gMoveFrom[0], gMoveFrom[1]] = null;
                if (getIsWhiteTurn())
                {
                    gBoard[gMoveFrom[0], (gMoveFrom[1] + ((gMoveFrom[1] > gWEnPassantPosition[1]) ? -1 : 1))] = null;
                }
                else
                {
                    gBoard[gMoveFrom[0], (gMoveFrom[1] + ((gMoveFrom[1] > gBEnPassantPosition[1]) ? -1 : 1))] = null;
                }
                gIsEnPassant = false;
            }

            else if (gMovingPiece is King && (colDifference == 2 || colDifference == -2)) //castling
            {
                if (colDifference > 0) //right rook didn't move?&& gBoard[gMoveFrom[0], gMoveDestination[1] + 1] != null //right rook there?
                                       //&& !gBoard[gMoveFrom[0], gMoveDestination[1] + 1].didPositionChange()
                {
                    gMovingPiece.setPosition(gMoveDestination);
                    gBoard[gMoveDestination[0], gMoveDestination[1]] = gMovingPiece;// move king
                    gBoard[gMoveFrom[0], gMoveFrom[1]] = null;
                    gMovingPiece = gBoard[gMoveFrom[0], gMoveDestination[1] + 1]; //get rook
                    gBoard[gMoveFrom[0], gMoveDestination[1] + 1] = null;
                    gMoveDestination = new int[] { gMoveFrom[0], gMoveDestination[1] - 1 }; //set destination
                    gBoard[gMoveDestination[0], gMoveDestination[1]] = gMovingPiece;// move rook
                    gMovingPiece.setPosition(gMoveDestination);
                    if (gMovingPiece.getName()[0] == 'W')
                        gWhiteCastled = true;
                    else
                        gBlackCastled = true;
                }
                else if (colDifference < 0)
                {
                    gMovingPiece.setPosition(gMoveDestination);
                    gBoard[gMoveDestination[0], gMoveDestination[1]] = gMovingPiece;// move king
                    gBoard[gMoveFrom[0], gMoveFrom[1]] = null;
                    gMovingPiece = gBoard[gMoveFrom[0], gMoveDestination[1] - 2]; //get rook
                    gBoard[gMoveFrom[0], gMoveDestination[1] - 2] = null;
                    gMoveDestination = new int[] { gMoveFrom[0], gMoveDestination[1] + 1 }; //set destination
                    gBoard[gMoveDestination[0], gMoveDestination[1]] = gMovingPiece;// move rook
                    gMovingPiece.setPosition(gMoveDestination);
                    if (gMovingPiece.getName()[0] == 'W')
                        gWhiteCastled = true;
                    else
                        gBlackCastled = true;
                }
            }
            else
            {
                gMovingPiece.setPosition(gMoveDestination);
                gBoard[gMoveDestination[0], gMoveDestination[1]] = gMovingPiece;
                gBoard[gMoveFrom[0], gMoveFrom[1]] = null;
            }
            gMovingPiece.setPositionChange(true);
            adjustBoard();
        }

        public bool checkCurrentMove(bool notCheck)
        {
            string destination = checkDestination(gMovingPiece.getName()); // returns "empty" "same color" "different color"
            if (destination == "same color")
            {
                return false;
            }
            if (isBlockedPath(gMovingPiece))
            {
                return false;
            }
            //castling 
            if (gMovingPiece.getName()[0] == 'W' && !gWhiteCastled ||
               gMovingPiece.getName()[0] == 'B' && !gBlackCastled)
            {
                int colDifference = gMoveDestination[1] - gMoveFrom[1];
                if (gMovingPiece is King && (colDifference == 2 || colDifference == -2))
                {
                    if (gMoveDestination[1] > 6 || gMoveDestination[1] < 2)
                        return false;
                    if (gMoveDestination[0] - gMoveFrom[0] != 0)
                        return false;
                    if (gCheck) //castling under check
                        return false;
                    Piece rookR = gBoard[gMoveFrom[0], gMoveDestination[1] + 1]; //regard color
                    Piece rookL = gBoard[gMoveFrom[0], gMoveDestination[1] - 2];
                    int[] kingsSquare = gMovingPiece.getPosition();
                    if (gMovingPiece.didPositionChange()) // king moved
                        return false;
                    if (colDifference > 0)
                    {
                        if (rookR == null || rookR.didPositionChange())//right rook there? moved?
                            return false;
                        --gMoveDestination[1];
                        if (lookForChecks(gMovingPiece.getName()[0] + ""))
                        {
                            gMovingPiece.setPositionChange(false);
                            gMovingPiece.setPosition(kingsSquare);
                            return false;
                        }
                        ++gMoveDestination[1];

                        //second move is checked in regular king move - self check a little later 
                    }
                    else if (colDifference < 0)
                    {
                        if (rookL == null || rookL.didPositionChange())
                            return false;
                        if (gBoard[rookL.getPosition()[0], rookL.getPosition()[1] + 1] != null)//check the spot next to left rook is empty
                            return false;
                        ++gMoveDestination[1];
                        if (lookForChecks(gMovingPiece.getName()[0] + ""))
                        {
                            gMovingPiece.setPositionChange(false);
                            return false;
                        }
                        --gMoveDestination[1];

                    }
                }
            }
            //check promotion
            if (gMovingPiece.getName() == "WP" && gMoveDestination[0] == 7)
            {
                promotion(notCheck);
                return true;
            }
            else if (gMovingPiece.getName() == "BP" && gMoveDestination[0] == 0)
            {
                promotion(notCheck);
                return true;
            }

            //if (gIsEnPassant && gMovingPiece.getName().EndsWith("P"))//check enPassant capture
            if (gIsEnPassant && (gMovingPiece.getName() == "WP" && gMoveDestination[0] == 5) ||//check enPassant capture
                (gMovingPiece.getName() == "BP" && gMoveDestination[0] == 2))
            {
                if (gMoveDestination[0] == ((gIsWhiteTurn) ? gWEnPassantPosition[0] : gBEnPassantPosition[0])
                 && gMoveDestination[1] == ((gIsWhiteTurn) ? gWEnPassantPosition[1] : gBEnPassantPosition[1]))
                {
                    return true;
                }

            }

            if (gMovingPiece.getName().EndsWith("P"))//check pawn capture move to an empty square
            {
                if (gMoveDestination[1] != gMoveFrom[1])
                    if (gBoard[gMoveDestination[0], gMoveDestination[1]] == null)
                    {
                        return false;
                    }

            }

            if (gMovingPiece.getName().EndsWith("P") && //pawn that captures while not moving diagonally
               (gMoveFrom[1] - gMoveDestination[1] == 0)
               && gBoard[gMoveDestination[0], gMoveDestination[1]] != null)
            {
                return false;
            }

            bool selfChecked = lookForChecks(gMovingPiece.getName()); //check if position after current move creates check for player
            if (selfChecked)
            {
                return false;
            }
            //check enPassant
            
            if (gMovingPiece.getName().EndsWith("P")
               && (gMoveFrom[0] - gMoveDestination[0] == 2
               || gMoveFrom[0] - gMoveDestination[0] == -2))
            {
                gEnPassantTurn = (gMovingPiece.getName().StartsWith("W")) ? "W" : "B";
                gIsEnPassant = true;
                gWEnPassantPosition[0] = ((gIsWhiteTurn) ? gWEnPassantPosition[0] : gMoveDestination[0] + 1);
                gWEnPassantPosition[1] = ((gIsWhiteTurn) ? gWEnPassantPosition[0] : gMoveDestination[1]);
                gBEnPassantPosition[0] = ((!gIsWhiteTurn) ? gBEnPassantPosition[0] : gMoveDestination[0] - 1);
                gBEnPassantPosition[1] = ((!gIsWhiteTurn) ? gBEnPassantPosition[0] : gMoveDestination[1]);
            }
            return true;
        }

        private void promotion(bool notCheck)
        {
            if (!notCheck)
                return;
            string ranks = "QRBN";
            string input = "";

            do
            {
                Console.WriteLine("Coronation Row! promotion options: Q R B N");
                input = Console.ReadLine().Trim().ToUpper();
            } while (input == "" || !ranks.Contains(input[0] + ""));

            switch (input[0])
            {
                case 'Q':
                    gMovingPiece = new Queen(gMovingPiece.getName()[0] + "Q", gMoveFrom);
                    break;
                case 'R':
                    gMovingPiece = new Rook(gMovingPiece.getName()[0] + "R", gMoveFrom);
                    break;
                case 'B':
                    gMovingPiece = new Bishop(gMovingPiece.getName()[0] + "B", gMoveFrom);
                    break;
                case 'N':
                    gMovingPiece = new Knight(gMovingPiece.getName()[0] + "N", gMoveFrom);
                    break;
                default:
                    promotion(notCheck);
                    break;
            }
        }

        public Board copyBoard()
        {
            int[] currentPosition = gMoveFrom;
            Board boardCopy = new Board("copy");
            boardCopy.gCheck = gCheck;
            boardCopy.gBlackCastled = gBlackCastled;
            boardCopy.gWhiteCastled = gWhiteCastled;
            boardCopy.gWEnPassantPosition[0] = gWEnPassantPosition[0];
            boardCopy.gWEnPassantPosition[1] = gWEnPassantPosition[1];
            boardCopy.gBEnPassantPosition[0] = gBEnPassantPosition[0];
            boardCopy.gBEnPassantPosition[1] = gBEnPassantPosition[1];
            boardCopy.gIsEnPassant = gIsEnPassant;
            boardCopy.gIsWhiteTurn = gIsWhiteTurn;
            for (int i = 0; i < gBoard.GetLength(0); i++)
            {
                for (int j = 0; j < gBoard.GetLength(1); j++)
                {
                    if (gBoard[i, j] != null) //copy piece
                    {
                        boardCopy.gBoard[i, j] = gBoard[i, j];
                        if (boardCopy.gBoard[i, j] is King)
                        {
                            if (boardCopy.gBoard[i, j].getName().StartsWith("W"))
                            {
                                boardCopy.gWK = boardCopy.gBoard[i, j];
                            }
                            else
                                boardCopy.gBK = boardCopy.gBoard[i, j];
                        }
                    }
                    if (i == currentPosition[0] && j == currentPosition[1]) //empty moving piece position
                        boardCopy.gBoard[i, j] = null;
                }

                boardCopy.gBoard[gMoveDestination[0], gMoveDestination[1]] = gMovingPiece; //position moving piece 
                boardCopy.gBoard[gMoveDestination[0], gMoveDestination[1]].setPosition(gMoveDestination);
            }
            return boardCopy; //board returned after a move was made
        }
        public bool lookForChecks(string name)
        {
            Board boardCopy = copyBoard();
            Piece piece;
            int[] kingsSquare = (name.StartsWith("W")) ? getWK().getPosition() : getBK().getPosition();
            int[] path = { };
            for (int i = 0; i < boardCopy.gBoard.GetLength(0); i++)
            {
                for (int j = 0; j < boardCopy.gBoard.GetLength(1); j++)
                {
                    piece = boardCopy.gBoard[i, j];
                    //Console.WriteLine("check? " + gCheck + " white? " + getIsWhiteTurn());
                    if (piece != null && !piece.getName().StartsWith(name[0]))
                    {
                        if (piece.getName().EndsWith('P') && !(piece.getPosition()[0] - kingsSquare[0] != 0 && //pawn threatens king only in capture move
                            piece.getPosition()[1] - kingsSquare[1] != 0))
                            continue;
                        if (piece.getPosition() == kingsSquare)//??the attacking pieces are its opponents! irrelevant
                        {
                            path = new int[] { i, j };
                            kingsSquare = new int[] { i, j };
                        }
                        else
                            path = piece.move(i, j, kingsSquare[0], kingsSquare[1]);
                        if (path.Length != 0)
                            if (!isBlockedPath(path, boardCopy))
                            {
                                return true;
                            }
                    }
                }
            }

            return false;
        }

        private bool isBlockedPath(Piece piece)
        {
            int[] currentPosition = gMovingPiece.getPosition();
            int[] path = piece.move(currentPosition[0], currentPosition[1], gMoveDestination[0], gMoveDestination[1]);
            return isBlockedPath(path, this);
        }

        private bool isBlockedPath(int[] path, Board board) //overload
        {
            if (path.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < path.Length - 2 && path.Length > 2; i += 2)
            {
                if (board.gBoard[path[i], path[i + 1]] != null)
                    return true;
            }
            return false;
        }

        private string checkDestination(string name)
        {
            if (gBoard[gMoveDestination[0], gMoveDestination[1]] == null)
                return "empty";
            if (gBoard[gMoveDestination[0], gMoveDestination[1]].getName().StartsWith(name[0]))
                return "same color";
            return "different color";
        }

        public bool isKingUnderChess()
        {
            return false;
        }

        private bool checkEnPassant()
        {
            return true;
        }
        public bool getCheck()
        {
            return gCheck;
        }

        public void setCheck(bool check)
        {
            gCheck = check;
        }
        public King getWK()
        {
            return (King)gWK;
        }

        public King getBK()
        {
            return (King)gBK;
        }
        public bool getIsWhiteTurn()
        {
            return gIsWhiteTurn;
        }

        public void changeIsWhiteTurn()
        {
            gIsWhiteTurn = !gIsWhiteTurn;
        }

        public void getMove()
        {
            string letters = "abcdefgh";
            string digits = "12345678";
            string playersMove;
            bool letter0, digit1, letter2, digit3;
            letter0 = digit1 = digit3 = letter2 = false;
            do
            {
                Console.WriteLine("Please enter your move and press ENTER (e.g e2e4)");
                playersMove = Console.ReadLine().Trim().ToLower();

                if (playersMove != null && playersMove.Length == 4)
                {
                    letter0 = letters.Contains(playersMove[0]);
                    digit1 = digits.Contains(playersMove[1]);
                    letter2 = letters.Contains(playersMove[2]);
                    digit3 = digits.Contains(playersMove[3]);
                }
                else
                    Console.WriteLine("A move length should be 4");
                if (!(letter0 && digit1 && letter2 && digit3))
                    Console.WriteLine("invalid letter or digit");
            } while (!(letter0 && digit1 && letter2 && digit3));

            int positionRow = digits.IndexOf(playersMove[1]);
            int positionCol = letters.IndexOf(playersMove[0]);
            int destinationRow = digits.IndexOf(playersMove[3]);
            int destinationCol = letters.IndexOf(playersMove[2]);
            gMoveDestination = new int[] { destinationRow, destinationCol };
            gMoveFrom = new int[] { positionRow, positionCol };
            gMovingPiece = gBoard[positionRow, positionCol];
            if (gMovingPiece == null)
            {
                Console.WriteLine("You chose no piece!");
                getMove();
            }
        }

        public void printBoard()
        {
            for (int row = gBoard.GetLength(0) - 1; row >= 0; row--)
            {
                for (int column = 0; column < gBoard.GetLength(0); column++)
                {
                    if (column == 0)
                        Console.Write(row + 1 + "  ");
                    if (gBoard[row, column] == null)
                        Console.Write("EE ");
                    else
                        Console.Write(gBoard[row, column] + " ");
                }
                Console.WriteLine();
                if (row == 0)
                    Console.WriteLine("    A  B  C  D  E  F  G  H");
            }
        }

        public bool legalMovesExist(string turn)
        {
            Piece piece;
            Board board = copyBoard();
            string nextTurn = (turn == "W") ? "B" : "W";

            int[] moves;
            for (int row = 0; row < board.gBoard.GetLength(0); row++)
            {
                for (int col = 0; col < board.gBoard.GetLength(1); col++)
                {
                    piece = board.gBoard[row, col];

                    if (piece != null && piece.getName().StartsWith(turn))
                    {
                        for (int rowto = 0; rowto < board.gBoard.GetLength(0); rowto++)
                        {
                            for (int colTo = 0; colTo < board.gBoard.GetLength(1); colTo++)
                            {
                                piece.setPosition(new int[] { row, col });
                                moves = piece.move(row, col, rowto, colTo);
                                
                                if (moves.Length > 0)
                                {
                                    board.gMovingPiece = piece;
                                    board.gMoveFrom = new int[] { row, col };
                                    board.gMoveDestination = new int[] { rowto, colTo };
                                    if(rowto == 5 && colTo == 6)
                                    {
                                        Console.WriteLine();
                                    }
                                    if (board.checkCurrentMove(false))
                                    {
                                        //Console.WriteLine("it is {0} turn", turn);
                                        //Console.WriteLine("from {0}{1} to {2}{3}", row, col, rowto, colTo);
                                        if (lookForChecks(nextTurn))
                                        {
                                            //Console.WriteLine("looking for mate attack it was {0} turn", turn);
                                            //Console.WriteLine("from {0}{1} to {2}{3}", row, col, rowto, colTo);
                                            continue;
                                        }
                                        return true;
                                    }
                                    
                                }
                            }

                        }

                    }
                }
            }
            return false;
        }

        public void adjustBoard()
        {
            for (int row = 0; row < gBoard.GetLength(0); row++)
            {
                for (int col = 0; col < gBoard.GetLength(1); col++)
                {
                    if (gBoard[row, col] != null)
                    {
                        bool moved = gBoard[row, col].didPositionChange();
                        gBoard[row, col].setPosition(new int[] { row, col });
                        gBoard[row, col].setPositionChange(moved);
                    }
                }
            }
        }
    }

    class Piece
    {
        string gName;
        int[] position;
        int value;
        bool positionChanged;

        public Piece(string name, int[] position)
        {
            setName(name);
            setPosition(position);
            positionChanged = false;
        }

        public bool didPositionChange()
        {
            return positionChanged;
        }
        public void setPositionChange(bool change)
        {
            positionChanged = change;
        }
        public void setValue(int value)
        {
            this.value = value;
        }
        public int getValue()
        {
            return this.value;
        }
        public void setPosition(int[] position)
        {
            this.position = position;
        }

        public int[] getPosition()
        {
            return position;
        }

        public void setName(string name)
        {
            this.gName = name;
        }
        public string getName()
        {
            return gName;
        }

        public override string ToString()
        {
            return getName();
        }

        public virtual int[] move(int positionRow, int positionCol, int destinationRow, int destinationCol)
        {
            //should go to std.err or std.log
            //Console.WriteLine("Base class Piece can't move. Override base class");
            return new int[] { };
        }
    }

    class Pawn : Piece
    {
        bool gIsDiagonal;
        bool gJumpedTwo; //for en passant

        public Pawn(string name, int[] position) : base(name, position)
        {
            setValue(1);
        }

        public bool hasEnPassant()
        {
            return gJumpedTwo;
        }
        public bool hasDiagonal()
        {
            return gIsDiagonal;
        }
        public override int[] move(int positionRow, int positionCol, int destinationRow, int destinationCol)
        {
            bool movedOne = moveOneForward(positionRow, positionCol, destinationRow, destinationCol);
            bool movedTwo = moveTwoForward(positionRow, positionCol, destinationRow, destinationCol);
            if (movedTwo)
            {
                int difference = (destinationRow - positionRow > 0) ? 1 : -1;
                return new int[] { destinationRow - difference, destinationCol, destinationRow, destinationCol };
            }

            if (movedOne || moveToCapturePiece(positionRow, positionCol, destinationRow, destinationCol)) // cancels jumpedTwo
                return new int[] { destinationRow, destinationCol };
            return new int[] { };
        }
        private bool moveOneForward(int positionRow, int positionCol, int destinationRow, int destinationCol)
        {
            int differenceRows = destinationRow - positionRow;
            int differenceCols = destinationCol - positionCol;
            if (differenceCols != 0)
                return false;
            if (getName() == "WP")
                return differenceRows == 1;
            return differenceRows == -1;
        }

        private bool moveTwoForward(int positionRow, int positionCol, int destinationRow, int destinationCol)
        {
            int differenceRows = destinationRow - positionRow;
            int differenceCols = destinationCol - positionCol;
            gJumpedTwo = true;
            if (!(positionRow == 1 || positionRow == 6))
                return false;
            if (differenceCols != 0)
                return false;
            if (getName() == "WP")
                return differenceRows == 2;
            return differenceRows == -2;
        }

        private bool moveToCapturePiece(int positionRow, int positionCol, int destinationRow, int destinationCol)
        {
            int differenceRows = destinationRow - positionRow;
            int differenceCols = destinationCol - positionCol;
            gIsDiagonal = true;
            gJumpedTwo = false;
            if (getName() == "WP")
                return differenceRows == 1 && differenceCols == 1 ||
                       differenceRows == 1 && differenceCols == -1;
            return differenceRows == -1 && differenceCols == 1 ||
                   differenceRows == -1 && differenceCols == -1;
        }
    }

    class Bishop : Pawn
    {
        public Bishop(string name, int[] position) : base(name, position)
        {
            setValue(3);
        }

        public override int[] move(int positionRow, int positionCol, int destinationRow, int destinationCol)
        {
            string name = getName();
            int differenceRow = destinationRow - positionRow;
            int differenceCol = destinationCol - positionCol;
            int[][] moves = new int[8][]; //7
            if (differenceRow == differenceCol || differenceRow == -differenceCol)
            {
                if (differenceRow > 0)
                {
                    setName("WP");
                }
                bool destination = false;
                for (int i = 0; i < moves.Length && !destination; i++)
                {
                    int nextPositionRow = (differenceRow > 0) ? positionRow + 1 : positionRow - 1;
                    int nextPositionCol = (differenceCol > 0) ? positionCol + 1 : positionCol - 1;
                    moves[i] = base.move(positionRow, positionCol, nextPositionRow, nextPositionCol);
                    if (moves[i][0] < 0)
                        moves[i][0] *= -1;
                    if (moves[i][1] < 0)
                        moves[i][1] *= -1;
                    positionRow = nextPositionRow;
                    positionCol = nextPositionCol;
                    if (moves[i][0] == destinationRow)
                        destination = true;
                }
            }
            setName(name);

            int size = 0;
            bool stop = false;
            for (int i = 0; i < moves.Length && !stop; i++)
            {
                if (moves[i] != null)
                    size++;
                else
                    stop = true;
            }

            if (size > 0)
                size *= 2;

            int[] path = new int[size];
            for (int i = 0, j = 0; i < path.Length - 1; i += 2, j++)
            {
                path[i] = moves[j][0];
                path[i + 1] = moves[j][1];
            }

            return path;
        }
    }
    class Rook : Pawn
    {
        public Rook(string name, int[] position) : base(name, position)
        {
            setValue(5);
        }

        public override int[] move(int positionRow, int positionCol, int destinationRow, int destinationCol)
        {
            int differenceRow = destinationRow - positionRow;
            int differenceCol = destinationCol - positionCol;
            if (differenceRow != 0 && differenceCol != 0)
                return new int[] { };
            string name = getName();
            int[][] moves = new int[8][]; //7

            if (differenceRow == 0)
            {
                differenceRow = differenceCol;
                int tmpCol = positionRow;
                positionRow = positionCol;
                destinationRow = destinationCol;
                destinationCol = positionCol = tmpCol;
            }

            if (differenceRow > 0)
            {
                setName("WP");
            }

            bool destination = false;
            for (int i = 0; i < moves.Length && !destination; i++)
            {
                int nextPositionRow = (differenceRow > 0) ? positionRow + 1 : positionRow - 1;
                moves[i] = base.move(positionRow, positionCol, nextPositionRow, positionCol);
                if (moves[i][0] < 0)
                    moves[i][0] *= -1;
                if (moves[i][1] < 0)
                    moves[i][1] *= -1;
                positionRow = (differenceRow > 0) ? positionRow + 1 : positionRow - 1;
                if (moves[i][0] == destinationRow)
                    destination = true;
            }
            setName(name);

            int size = 0;
            bool stop = false;
            for (int i = 0; i < moves.Length && !stop; i++)
            {
                if (moves[i] != null)
                    size++;
                else
                    stop = true;
            }

            if (size > 0)
                size *= 2;

            int[] path = new int[size];
            for (int i = 0, j = 0; i < path.Length - 1; i += 2, j++)
            {
                path[i] = (differenceCol == 0) ? moves[j][0] : moves[j][1];
                path[i + 1] = (differenceCol == 0) ? moves[j][1] : moves[j][0];
            }

            return path;
        }

    }

    class King : Bishop
    {
        public King(string name, int[] position) : base(name, position)
        {
            setValue(0);
        }
        public override int[] move(int positionRow, int positionCol, int destinationRow, int destinationCol)
        {
            int differenceRow = destinationRow - positionRow;
            int differenceCol = destinationCol - positionCol;
            int[] moves = { };
            if ((differenceRow == 1 || differenceRow == -1) && (differenceCol == 1 || differenceCol == -1))//diagonal
                return new int[] { destinationRow, destinationCol };
            if ((differenceRow == 1 || differenceRow == -1) && differenceCol == 0)//row move
                return new int[] { destinationRow, destinationCol };
            if ((differenceCol == 1 || differenceCol == -1) && differenceRow == 0)//col move
                return new int[] { destinationRow, destinationCol };
            if (differenceRow != 0 && (differenceCol > 1 || differenceCol < -1))
                return new int[] { };

            //castling
            if (differenceRow == 0 && (differenceCol == 2 || differenceCol == -2 && !this.didPositionChange()))//&& ((getPosition()[0] == 0 && getPosition()[1] == 4 && getName()=="WK")|| (getPosition()[1] == 0) && getPosition()[1] == 4 && getName() == "BK"))
            {
                moves = new Rook(getName(), new int[] { positionRow, positionCol }).move(positionRow, positionCol, destinationRow, destinationCol);
                return moves;
            }


            return moves;
        }


    }

    class Queen : Bishop
    {
        public Queen(string name, int[] position) : base(name, position)
        {
            setValue(9);
        }

        public override int[] move(int positionRow, int positionCol, int destinationRow, int destinationCol)
        {
            int[] diagonalMove = base.move(positionRow, positionCol, destinationRow, destinationCol);
            Rook kingsRook = new Rook(getName(), new int[] { positionRow, positionCol });
            int[] horizonalVerticalMove = kingsRook.move(positionRow, positionCol, destinationRow, destinationCol);
            return diagonalMove.Length != 0 ? diagonalMove : horizonalVerticalMove;
        }
    }

    class Knight : Bishop
    {
        public Knight(string name, int[] position) : base(name, position)
        {
            setValue(3);
        }

        public override int[] move(int positionRow, int positionCol, int destinationRow, int destinationCol)
        {
            int differenceRow = destinationRow - positionRow;
            int differenceCol = destinationCol - positionCol;

            int[] moves = null;
            if ((differenceRow == 1 || differenceRow == -1) && (differenceCol == 2 || differenceCol == -2))
            {
                int posColTmp = differenceCol > 0 ? destinationCol - 1 : destinationCol + 1;
                int[] nextPosition = base.move(positionRow, positionCol, destinationRow, posColTmp);
                moves = new Rook(getName(), new int[] { nextPosition[0], nextPosition[1] }).move(nextPosition[0], nextPosition[1], destinationRow, destinationCol);
            }
            else if ((differenceCol == 1 || differenceCol == -1) && (differenceRow == 2 || differenceRow == -2))
            {
                int posRowTmp = differenceRow > 0 ? destinationRow - 1 : destinationRow + 1;//0122:0112
                int[] nextPosition = base.move(positionRow, positionCol, posRowTmp, destinationCol);//12
                moves = new Rook(getName(), new int[] { nextPosition[0], nextPosition[1] }).move(nextPosition[0], nextPosition[1], destinationRow, destinationCol);
            }
            else
                return new int[] { };
            return moves;
        }
    }

}
