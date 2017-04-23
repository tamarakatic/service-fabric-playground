using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Game
{
    [DataContract]
    public class ActorState
    {
        [DataMember] public List<Tuple<long, string>> Players;
        [DataMember] public int NextPlayerIndex;
        [DataMember] public int NumberOfMoves;
        [DataMember] public int[] Board;
        [DataMember] public string Winner;
    }
}
