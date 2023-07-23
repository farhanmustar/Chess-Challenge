using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            // Always promote to queen
            if (move.IsPromotion && move.PromotionPieceType == PieceType.Queen) {
              moveToPlay = move;
              break;
            }

            // Find highest value capture
            int movedPieceValue = pieceValues[(int)move.MovePieceType];
            int capturedPieceValue = pieceValues[(int)move.CapturePieceType];
            if (board.SquareIsAttackedByOpponent(move.TargetSquare)) {
              capturedPieceValue = capturedPieceValue - movedPieceValue;
            }
            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }

        return moveToPlay;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}
