using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
public class Lithium : IChessBot{
    static int[] PieceValue = {0, 100, 300, 300, 500, 1000, 0};
    static Dictionary<ulong, int>TTable = new Dictionary<ulong, int>();
    static List<Move>OrderMoves(Move[] moves){
        List<Move> orderedMoves = new List<Move>();
        foreach(Move move in moves){
            if(move.IsCapture || move.IsPromotion){
                orderedMoves.Insert(0, move);
            }else{
                orderedMoves.Add(move);
            }
        }
        return orderedMoves;
    }
    static int[] DistanceToEdge = {0,1,2,3,3,2,1,0};
    static Func<Square,int>[] PSQT = {
                                      sq => 0, //null
                                      sq => sq.Rank*10-10+(sq.Rank==1?50:0)+(DistanceToEdge[sq.Rank]==3&&DistanceToEdge[sq.File]==3?30:0), //pawn
                                      sq => (DistanceToEdge[sq.Rank]+DistanceToEdge[sq.File])*10-40, //knight
                                      sq => (DistanceToEdge[sq.Rank]+DistanceToEdge[sq.File])*10-40, //bishop
                                      sq => sq.Rank==6?10:0+((sq.Rank==0&&DistanceToEdge[sq.File]==3)?10:0), //rook
                                      sq => (DistanceToEdge[sq.Rank]+DistanceToEdge[sq.File])*5-10, //queen
                                      sq => (3-DistanceToEdge[sq.Rank]+3-DistanceToEdge[sq.File])*10-5-(sq.Rank>1?50:0) //king
                                     };
    static int Eval(Board board){
        if(board.IsDraw()){return 0;}
        if(board.IsInCheckmate()){return 320000 * (board.IsWhiteToMove ? 1 : -1);}
        PieceList[] pieceList = board.GetAllPieceLists();
        int material = 0;
        int psqt = 0;
        foreach(PieceList list in pieceList){
            material += PieceValue[(int)list.TypeOfPieceInList]*list.Count*(list.IsWhitePieceList ? 1 : -1);
        }
        for(int i=0;i<64;i++){
            Square sq = new Square(i);
            Piece p = board.GetPiece(sq);
            if(!p.IsWhite)sq = new Square(i^56);
            psqt+=(PSQT[(int)p.PieceType](sq))*(p.IsWhite?1:-1);
        }
        return ((material+psqt)*10+board.GetLegalMoves().Length)*(board.IsWhiteToMove ? 1 : -1);
    }
    static int Max(int a, int b){return a>b?a:b;}
    static int Search(Board board, int depth, int alpha, int beta){
        if(depth == 0){
            if(TTable.ContainsKey(board.ZobristKey)){
                return TTable[board.ZobristKey];
            }
            else{
                TTable.Add(board.ZobristKey, Eval(board));
                return Eval(board);
            }
        }
        Move[] moves = board.GetLegalMoves();
        List<Move> orderedMoves = OrderMoves(moves);
        foreach(Move move in orderedMoves){
            board.MakeMove(move);
            int CurrEval = -Search(board, depth-1, -beta, -alpha);
            board.UndoMove(move);
            if(CurrEval >= beta){
                return beta;
            }
            alpha = Max(alpha, CurrEval);
        }
        return alpha;
    }
    public Move Think(Board board, Timer timer){
        Move[] moves = board.GetLegalMoves();
        int MaxEval = -320001;
        Move MaxMove = new Move();
        int CurrDepth = 1;
        Random rng=new();
        int time = timer.MillisecondsRemaining / 50;
        while(true){
            foreach(Move move in moves){
                board.MakeMove(move);
                int CurrEval = -Search(board, CurrDepth, -320000, 320000);
                if (MaxEval < CurrEval){
                    MaxEval = CurrEval;
                    MaxMove = move;
                }
                board.UndoMove(move);
            }
            if(timer.MillisecondsElapsedThisTurn > time){
                if(MaxMove!=Move.NullMove)
                    return MaxMove;
                else return moves[rng.Next(moves.Length)];
            }
            CurrDepth++;
        }
    }
}