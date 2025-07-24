using System;

namespace SharpMSDF.Core
{
    public class EdgeHolder
    {
        private EdgeSegment? edgeSegment;

        public EdgeHolder() { }

        public EdgeHolder(EdgeSegment segment)
        {
            edgeSegment = segment;
        }

        // Copy constructor
        public EdgeHolder(EdgeHolder orig)
        {
            edgeSegment = orig.edgeSegment != null ? orig.edgeSegment.Clone() : null;
        }

        // Move constructor equivalent (not idiomatic in C#, but retained per translation request)
        public static EdgeHolder Move(EdgeHolder orig)
        {
            var moved = new EdgeHolder();
            moved.edgeSegment = orig.edgeSegment;
            orig.edgeSegment = null;
            return moved;
        }

        // Swap function
        public static void Swap(ref EdgeHolder a, ref EdgeHolder b)
        {
            (a, b) = (b, a);
        }

        // Assignment
        public EdgeHolder Assign(EdgeHolder orig)
        {
            if (!ReferenceEquals(this, orig))
            {
                if (edgeSegment != null)
                    edgeSegment = null;

                edgeSegment = orig.edgeSegment != null ? orig.edgeSegment.Clone() : null;
            }
            return this;
        }

        // Move assignment
        public EdgeHolder AssignMove(EdgeHolder orig)
        {
            if (!ReferenceEquals(this, orig))
            {
                if (edgeSegment != null)
                    edgeSegment = null;

                edgeSegment = orig.edgeSegment;
                orig.edgeSegment = null;
            }
            return this;
        }

        // Operators
        public EdgeSegment Get()
        {
            return edgeSegment;
        }

        public void Set(EdgeSegment segment)
        {
            edgeSegment = segment;
        }

        public EdgeSegment Segment => edgeSegment;

        public static implicit operator EdgeHolder(EdgeSegment edgeSegment)
        {
            return new EdgeHolder(edgeSegment);
        }
        
        public static implicit operator EdgeSegment(EdgeHolder holder)
        {
            return holder.edgeSegment;
        }

        public static implicit operator bool(EdgeHolder holder)
        {
            return holder != null && holder.edgeSegment != null;
        }
    }
}
