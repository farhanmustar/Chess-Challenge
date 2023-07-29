using ChessChallenge.API;
using System.Linq;
using System.Collections.Generic;
using System;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 1000 };
    PieceType[] pinnablePiece = {PieceType.Bishop, PieceType.Rook, PieceType.Queen};

    // TODO: check using queen while being attacked. why??
    // TODO: check protected by other type. or filter to exclude queen. cause queen need to be mobile?

    // TODO: save queen if it is attacked (or rook and others too.
    // TODO: check attacked piece and see if can move to protect it
    public Move Think(Board board, Timer timer)
    {
        bool is_white = board.IsWhiteToMove;
        // TODO: use nonAlloc type
        Move[] allMoves = board.GetLegalMoves();
        PieceList[] pieceLists = board.GetAllPieceLists();
        var movesData = new Dictionary<Move, double>();

        Random rng = new();

        foreach (Move move in allMoves)
        {
          // Initialize in some randomness incase no clear option
          movesData[move] = rng.Next(100);

          // Always play checkmate in one
          if (MoveIsCheckmate(board, move))
          {
              return move;
          }

          // TODO: check who move is attacked by this add values also.
          PieceType isAttacking = MoveIsAttacking(board, move);

          // TODO: add progressive move score... half the piece value for other type
          // for pawn is based on rank normalized.

          if (move.MovePieceType == PieceType.Pawn && 
              MoveIsRankUp(board, move) &&
              (MoveIsProtected(board, move) || !MoveIsAttacked(board, move))) {
            movesData[move] += (pieceValues[(int)move.MovePieceType] * NormalizeRank(board, move.TargetSquare)) / 2;
          }

          if (move.MovePieceType != PieceType.Pawn &&
              MoveIsCheck(board, move) && !MoveIsAttacked(board, move)) {
            movesData[move] += pieceValues[(int)PieceType.King] -
              pieceValues[(int)move.MovePieceType];
          }

          if (NotMoveIsAttacked(board, move)) {
            if (!MoveIsAttacked(board, move) && MoveIsProtected(board, move)) {
              movesData[move] += pieceValues[(int)move.MovePieceType] * 3;
            } else if (!MoveIsAttacked(board, move)) {
              movesData[move] += pieceValues[(int)move.MovePieceType] * 2;
            } else if (NotMoveIsProtected(board, move)) {
              movesData[move] -= pieceValues[(int)PieceType.King] - pieceValues[(int)move.MovePieceType];
            }
          }

          if (MoveIsCapture(board, move)) {
            if (!MoveIsAttacked(board, move) && (move.MovePieceType != PieceType.Pawn || MoveIsProtected(board, move))) {
              movesData[move] += pieceValues[(int)move.CapturePieceType] * 5;

            } else if (MoveIsAttacked(board, move) && MoveIsProtected(board, move)) {
              movesData[move] += pieceValues[(int)move.CapturePieceType] * 2 - pieceValues[(int)move.MovePieceType] * 3;
            } else {
              movesData[move] += pieceValues[(int)move.CapturePieceType] * 1 - pieceValues[(int)move.MovePieceType] * 3;
            }

            if (isAttacking != PieceType.None && isAttacking > move.CapturePieceType) {
              // Is pinning the capture piece with attacking piece.
              movesData[move] -= pieceValues[(int)isAttacking] * 2;
            }
          }

          if (pinnablePiece.Contains(move.MovePieceType)) {
            PieceType isPinning = MoveIsPinning(board, move);
            if (isPinning > PieceType.Knight) {
              if (!MoveIsAttacked(board, move)) {
                movesData[move] += pieceValues[(int)isPinning] * 0.5;
              }
            }
          }

          if (isAttacking != PieceType.None) {
            if (!MoveIsAttacked(board, move) || MoveIsProtected(board, move)) {
              movesData[move] += pieceValues[(int)isAttacking] - pieceValues[(int)move.MovePieceType];
            }
          }

          if (move.IsPromotion && move.PromotionPieceType == PieceType.Queen) {
            Console.WriteLine("queen promotion");
            if (!MoveIsAttacked(board, move)) {
              movesData[move] += pieceValues[(int)move.PromotionPieceType] * 10;
            } else if (MoveIsProtected(board, move)) {
              movesData[move] += pieceValues[(int)move.PromotionPieceType] * 5;
            } else {
              movesData[move] += pieceValues[(int)move.PromotionPieceType] * 3;
            }
          }
          if (move.IsPromotion && move.PromotionPieceType != PieceType.Queen) {
            movesData[move] = 0;
          }
          if (move.MovePieceType == PieceType.King) {
            movesData[move] -= pieceValues[(int)move.MovePieceType] * 0.1;
          }
        }
        return movesData.MaxBy(kv => kv.Value).Key;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool MoveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }

    bool MoveIsRankUp(Board board, Move move)
    {
      return NormalizeRank(board, move.TargetSquare) > NormalizeRank(board, move.StartSquare);
    }

    bool MoveIsProtected(Board board, Move move)
    {
      bool p = false;
      board.MakeMove(move);
      p = board.SquareIsAttackedByOpponent(move.TargetSquare);
      board.UndoMove(move);
      return p;
    }

    bool NotMoveIsProtected(Board board, Move move)
    {
      bool p = false;
      if (board.TrySkipTurn()) {
        p = board.SquareIsAttackedByOpponent(move.StartSquare);
        board.UndoSkipTurn();
      }
      return p;
    }

    bool MoveIsAttacked(Board board, Move move)
    {
      return board.SquareIsAttackedByOpponent(move.TargetSquare);
    }

    bool NotMoveIsAttacked(Board board, Move move)
    {
      return board.SquareIsAttackedByOpponent(move.StartSquare);
    }

    bool MoveIsCapture(Board board, Move move)
    {
      return move.CapturePieceType != PieceType.None;
    }

    PieceType MoveIsAttacking(Board board, Move move) {
      PieceType isAttacking = PieceType.None;
      board.MakeMove(move);
      if(board.TrySkipTurn()) {
        Move[] allMoves = board.GetLegalMoves(true);
        foreach (Move nextMove in allMoves)
        {
          if (move.TargetSquare != nextMove.StartSquare || MoveIsAttacked(board, nextMove)) {
            continue;
          }
          if (nextMove.CapturePieceType > isAttacking) {
            isAttacking = nextMove.CapturePieceType;
          }
        }
        board.UndoSkipTurn();
      } else {
        isAttacking = PieceType.King;
      }
      board.UndoMove(move);
      return isAttacking;
    }

    PieceType MoveIsPinning(Board board, Move move) {
      PieceType isPinning = PieceType.None;
      board.MakeMove(move);
      if(board.TrySkipTurn()) {
        Move[] allMoves = board.GetLegalMoves(true);
        foreach (Move nextMove in allMoves)
        {
          if (move.TargetSquare != nextMove.StartSquare || MoveIsAttacked(board, nextMove)) {
            continue;
          }
          PieceType isAttacking = MoveIsAttacking(board, nextMove);
          if (isAttacking <= nextMove.CapturePieceType) {
            continue;
          }

          if (isPinning < isAttacking) {
            isPinning = isAttacking;
          }
        }
        board.UndoSkipTurn();
      } else {
        // next move is check so no pinning
        isPinning = PieceType.None;
      }
      board.UndoMove(move);
      return isPinning;
    }

    int NormalizeRank(Board board, Square square) {
      if (board.IsWhiteToMove) {
        return square.Rank;
      }
      return 7 - square.Rank;
    }
}
