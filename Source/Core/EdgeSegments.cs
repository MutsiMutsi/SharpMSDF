using System;

namespace SharpMSDF.Core
{
    public abstract class EdgeSegment
    {
        public EdgeColor Color;

        public const int MSDFGEN_CUBIC_SEARCH_STARTS = 4;
        public const int MSDFGEN_CUBIC_SEARCH_STEPS = 4;

        protected EdgeSegment(EdgeColor color = EdgeColor.White)
        {
            Color = color;
        }

        public static EdgeSegment Create(Vector2 p0, Vector2 p1, EdgeColor edgeColor = EdgeColor.White)
            => new LinearSegment(p0, p1, edgeColor);

        public static EdgeSegment Create(Vector2 p0, Vector2 p1, Vector2 p2, EdgeColor edgeColor = EdgeColor.White)
        {
            if (Vector2.Cross(p1 - p0, p2 - p1) == 0)
                return new LinearSegment(p0, p2, edgeColor);
            return new QuadraticSegment(p0, p1, p2, edgeColor);
        }

        public static EdgeSegment Create(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, EdgeColor edgeColor = EdgeColor.White)
        {
            Vector2 p12 = p2 - p1;
            if (Vector2.Cross(p1 - p0, p12) == 0 && Vector2.Cross(p12, p3 - p2) == 0)
                return new LinearSegment(p0, p3, edgeColor);
            if ((p12 = 1.5 * (p1) - 0.5 * (p0)) == 1.5 * (p2) - 0.5 * (p3))
                return new QuadraticSegment(p0, p12, p3, edgeColor);
            return new CubicSegment(p0, p1, p2, p3, edgeColor);
        }

        public abstract EdgeSegment Clone();
        public abstract int Type();
        public abstract Vector2[] ControlPoints();
        public abstract Vector2 Point(double param);
        public abstract Vector2 Direction(double param);
        public abstract Vector2 DirectionChange(double param);
        public abstract SignedDistance SignedDistance(Vector2 origin, out double param);
        public virtual void DistanceToPerpendicularDistance(ref SignedDistance distance, Vector2 origin, double param)
        {
            if (param < 0)
            {
                Vector2 dir = Direction(0).Normalize();
                Vector2 aq = new Vector2(origin.X - Point(0).X, origin.Y - Point(0).Y );
                double ts = Vector2.Dot(aq, dir);
                if (ts < 0)
                {
                    double perp = Vector2.Cross(aq, dir);
                    if (Math.Abs(perp) <= Math.Abs(distance.Distance))
                    {
                        distance.Distance = perp;
                        distance.Dot = 0;
                    }
                }
            }
            else if (param > 1)
            {
                Vector2 dir = Direction(1).Normalize();
                Vector2 bq = new Vector2( origin.X - Point(1).X, origin.Y - Point(1).Y );
                double ts = Vector2.Dot(bq, dir);
                if (ts > 0)
                {
                    double perp = Vector2.Cross(bq, dir);
                    if (Math.Abs(perp) <= Math.Abs(distance.Distance))
                    {
                        distance.Distance = perp;
                        distance.Dot = 0;
                    }
                }
            }
        }
        public abstract int ScanlineIntersections(double[] x, int[] dy, double y);
        public abstract void Bound(ref double l, ref double b, ref double r, ref double t);
        public abstract void Reverse();
        public abstract void MoveStartPoint(Vector2 to);
        public abstract void MoveEndPoint(Vector2 to);
        public abstract void SplitInThirds(out EdgeSegment part0, out EdgeSegment part1, out EdgeSegment part2);
    }

    public class LinearSegment : EdgeSegment
    {
        public const int EDGE_TYPE = 1;
        public readonly Vector2[] P = new Vector2[2];

        public LinearSegment(Vector2 p0, Vector2 p1, EdgeColor c = EdgeColor.White) : base(c)
        {
            P[0] = p0; P[1] = p1;
        }
        public override EdgeSegment Clone() => new LinearSegment(P[0], P[1], Color);
        public override int Type() => EDGE_TYPE;
        public override Vector2[] ControlPoints() => P;
        public override Vector2 Point(double t) => Arithmetic.Mix(P[0], P[1], t);
        public override Vector2 Direction(double t) => P[1] - P[0];
        public override Vector2 DirectionChange(double t) => new Vector2 { X = 0, Y = 0 };
        public double Length() => (P[1] - P[0]).Length();

        public override SignedDistance SignedDistance(Vector2 origin, out double t)
        {
            Vector2 aq = new Vector2 (origin.X - P[0].X, origin.Y - P[0].Y);
            Vector2 ab = P[1] - P[0];
            t = Vector2.Dot(aq, ab) / Vector2.Dot(ab, ab);
            Vector2 end = (t > .5) ? P[1] : P[0];
            double endpointDist = new Vector2( origin.X - end.X, origin.Y - end.Y).Length();
            if (t > 0 && t < 1)
            {
                // TODO : sus (GetOrthonormal had false as param)
                double ortho = Vector2.Dot(ab.GetOrthonormal(), aq);
                if (Math.Abs(ortho) < endpointDist)
                    return new SignedDistance(ortho, 0);
            }
            double sign = Arithmetic.NonZeroSign(Vector2.Cross(aq, ab));
            return new SignedDistance(sign * endpointDist,
                Math.Abs(Vector2.Dot(ab.Normalize(), new Vector2 (origin.X - end.X, origin.Y - end.Y).Normalize())));
        }

        public override int ScanlineIntersections(double[] x, int[] dy, double y)
        {
            if ((y >= P[0].Y && y < P[1].Y) || (y >= P[1].Y && y < P[0].Y))
            {
                double t = (y - P[0].Y) / (P[1].Y - P[0].Y);
                x[0] = Arithmetic.Mix(P[0].X, P[1].X, t);
                dy[0] = Arithmetic.Sign(P[1].Y - P[0].Y);
                return 1;
            }
            return 0;
        }

        public override void Bound(ref double l, ref double b, ref double r, ref double t)
        {
            if (P[0].X < l) l = P[0].X;
            if (P[0].Y < b) b = P[0].Y;
            if (P[0].X > r) r = P[0].X;
            if (P[0].Y > t) t = P[0].Y;
            if (P[1].X < l) l = P[1].X;
            if (P[1].Y < b) b = P[1].Y;
            if (P[1].X > r) r = P[1].X;
            if (P[1].Y > t) t = P[1].Y;
        }

        public override void Reverse()
        {
            var tmp = P[0];
            P[0] = P[1];
            P[1] = tmp;
        }
        public override void MoveStartPoint(Vector2 to) => P[0] = to;
        public override void MoveEndPoint(Vector2 to) => P[1] = to;

        public override void SplitInThirds(out EdgeSegment part0, out EdgeSegment part1, out EdgeSegment part2)
        {
            part0 = new LinearSegment(P[0], Point(1.0 / 3.0), Color);
            part1 = new LinearSegment(Point(1.0 / 3.0), Point(2.0 / 3.0), Color);
            part2 = new LinearSegment(Point(2.0 / 3.0), P[1], Color);
        }
    }

    public class QuadraticSegment : EdgeSegment
    {
        public const int EDGE_TYPE = 2;
        public readonly Vector2[] P = new Vector2[3];

        public QuadraticSegment(Vector2 p0, Vector2 p1, Vector2 p2, EdgeColor c = EdgeColor.White) : base(c)
        {
            P[0] = p0; P[1] = p1; P[2] = p2;
        }
        public override EdgeSegment Clone() => new QuadraticSegment(P[0], P[1], P[2], Color);
        public override int Type() => EDGE_TYPE;
        public override Vector2[] ControlPoints() => P;
        public override Vector2 Point(double t)
            => Arithmetic.Mix(Arithmetic.Mix(P[0], P[1], t), Arithmetic.Mix(P[1], P[2], t), t);
        public override Vector2 Direction(double t)
        {
            Vector2 tangent = Arithmetic.Mix(P[1] - P[0], P[2] - P[1], t);
            return tangent.Length() == 0 ? P[2] - P[0] : tangent;
        }
        public override Vector2 DirectionChange(double t)
            => new Vector2
            {
                X = (P[2] - P[1]).X - (P[1] - P[0]).X,
                Y = (P[2] - P[1]).Y - (P[1] - P[0]).Y
            };

        public double Length()
        {
            Vector2 ab = P[1] - P[0];
            Vector2 br = (P[2] - P[1]) - ab;
            double abab = Vector2.Dot(ab, ab), abbr = Vector2.Dot(ab, br), brbr = Vector2.Dot(br, br);
            double abLen = Math.Sqrt(abab), brLen = Math.Sqrt(brbr);
            double crs = Vector2.Cross(ab, br);
            double h = Math.Sqrt(abab + 2 * abbr + brbr);
            return (
                brLen * ((abbr + brbr) * h - abbr * abLen) +
                crs * crs * Math.Log((brLen * h + abbr + brbr) / (brLen * abLen + abbr))
            ) / (brbr * brLen);
        }
        public override SignedDistance SignedDistance(Vector2 origin, out double param)
        {
            // compute helper vectors
            Vector2 qa = P[0] - origin;
            Vector2 ab = P[1] - P[0];
            Vector2 br = (P[2] - P[1]) - ab;

            // cubic coefficients for |Q(t)|² derivative = 0
            double a = Vector2.Dot(br, br);
            double b = 3 * Vector2.Dot(ab, br);
            double c = 2 * Vector2.Dot(ab, ab) + Vector2.Dot(qa, br);
            double d = Vector2.Dot(qa, ab);

            // solve for t in [0,1]
            double[] t = new double[3];
            int solutions = EquationSolver.SolveCubic(t, a, b, c, d);

            // start by assuming the closest is at t=0 (Point A)
            Vector2 epDir = Direction(0);
            double minDistance = Arithmetic.NonZeroSign(Vector2.Cross(epDir, qa)) * qa.Length();
            param = -Vector2.Dot(qa, epDir) / Vector2.Dot(epDir, epDir);

            // check endpoint B (t=1)
            epDir = Direction(1);
            double distB = (new Vector2 (P[2].X - origin.X, P[2].Y - origin.Y)).Length();
            if (distB < Math.Abs(minDistance))
            {
                minDistance = Arithmetic.NonZeroSign(Vector2.Cross(epDir, new Vector2 (P[2].X - origin.X, P[2].Y - origin.Y))) * distB;
                param = Vector2.Dot(new Vector2 (origin.X - P[1].X, origin.Y - P[1].Y), epDir)
                        / Vector2.Dot(epDir, epDir);
            }

            // check interior critical points
            for (int i = 0; i < solutions; ++i)
            {
                if (t[i] > 0 && t[i] < 1)
                {
                    // Q(t) = qa + 2t·ab + t²·br
                    Vector2 qe = new Vector2
                    (
                        qa.X + 2 * t[i] * ab.X + t[i] * t[i] * br.X,
                        qa.Y + 2 * t[i] * ab.Y + t[i] * t[i] * br.Y
                    );
                    double dist = qe.Length();
                    if (dist <= Math.Abs(minDistance))
                    {
                        minDistance = Arithmetic.NonZeroSign(Vector2.Cross(ab + t[i] * br, qe)) * dist;
                        param = t[i];
                    }
                }
            }

            // choose return form depending on where the closest t lies
            if (param >= 0 && param <= 1)
            {
                return new SignedDistance(minDistance, 0);
            }
            else if (param < 0.5)
            {
                var dir0 = Direction(0).Normalize();
                return new SignedDistance(
                    minDistance,
                    Math.Abs(Vector2.Dot(dir0, qa.Normalize()))
                );
            }
            else
            {
                var dir1 = Direction(1).Normalize();
                var bq = new Vector2 (P[2].X - origin.X, P[2].Y - origin.Y).Normalize();
                return new SignedDistance(
                    minDistance,
                    Math.Abs(Vector2.Dot(dir1, bq))
                );
            }
        }

        public override int ScanlineIntersections(double[] x, int[] dy, double y)
        {
            // … port full C++ scanlineIntersections logic here …
            throw new NotImplementedException("QuadraticSegment.ScanlineIntersections()");
        }

        public override void Bound(ref double l, ref double b, ref double r, ref double t)
        {
            // … port full C++ bound logic here …
            throw new NotImplementedException("QuadraticSegment.Bound()");
        }

        public override void Reverse()
        {
            var tmp = P[0]; P[0] = P[2]; P[2] = tmp;
        }

        public override void MoveStartPoint(Vector2 to)
        {
            // … port C++ logic …
            throw new NotImplementedException("QuadraticSegment.MoveStartPoint()");
        }
        public override void MoveEndPoint(Vector2 to)
        {
            // … port C++ logic …
            throw new NotImplementedException("QuadraticSegment.MoveEndPoint()");
        }

        public override void SplitInThirds(out EdgeSegment part0, out EdgeSegment part1, out EdgeSegment part2)
        {
            // … port C++ logic …
            throw new NotImplementedException("QuadraticSegment.SplitInThirds()");
        }

        public EdgeSegment ConvertToCubic() =>
            new CubicSegment(P[0],
                             Arithmetic.Mix(P[0], P[1], 2.0 / 3.0),
                             Arithmetic.Mix(P[1], P[2], 1.0 / 3.0),
                             P[2],
                             Color);
    }

    public class CubicSegment : EdgeSegment
    {
        public const int EDGE_TYPE = 3;
        public readonly Vector2[] P = new Vector2[4];

        public CubicSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, EdgeColor c = EdgeColor.White) : base(c)
        {
            P[0] = p0; P[1] = p1; P[2] = p2; P[3] = p3;
        }
        public override EdgeSegment Clone() => new CubicSegment(P[0], P[1], P[2], P[3], Color);
        public override int Type() => EDGE_TYPE;
        public override Vector2[] ControlPoints() => P;
        public override Vector2 Point(double t)
        {
            Vector2 p12 = Arithmetic.Mix(P[1], P[2], t);
            return (Vector2)Arithmetic.Mix(Arithmetic.Mix(Arithmetic.Mix(P[0], P[1], t), p12, t),
                               Arithmetic.Mix(p12, Arithmetic.Mix(P[2], P[3], t), t), t);
        }
        public override Vector2 Direction(double t)
        {
            Vector2 tangent = Arithmetic.Mix(Arithmetic.Mix(P[1] - P[0], P[2] - P[1], t),
                                   Arithmetic.Mix(P[2] - P[1], P[3] - P[2], t), t);
            if (tangent.Length() == 0)
            {
                if (t == 0) return P[2] - P[0];
                if (t == 1) return P[3] - P[1];
            }
            return tangent;
        }
        public override Vector2 DirectionChange(double t) =>
            Arithmetic.Mix((P[2] - P[1]) - (P[1] - P[0]),
                (P[3] - P[2]) - (P[2] - P[1]), t);

        public override SignedDistance SignedDistance(Vector2 origin, out double t)
        {
            // … port full C++ iterative search logic …
            throw new NotImplementedException("CubicSegment.SignedDistance()");
        }

        public override int ScanlineIntersections(double[] x, int[] dy, double y)
        {
            // … port full C++ scanlineIntersections logic …
            throw new NotImplementedException("CubicSegment.ScanlineIntersections()");
        }

        public override void Bound(ref double l, ref double b, ref double r, ref double t)
        {
            // … port full C++ bound logic …
            throw new NotImplementedException("CubicSegment.Bound()");
        }

        public override void Reverse()
        {
            var tmp0 = P[0]; P[0] = P[3]; P[3] = tmp0;
            var tmp1 = P[1]; P[1] = P[2]; P[2] = tmp1;
        }

        public override void MoveStartPoint(Vector2 to)
        {
            P[1] = new Vector2(P[1].X + (to.X - P[0].X), P[1].Y + (to.Y - P[0].Y));
            P[0] = to;
        }
        public override void MoveEndPoint(Vector2 to)
        {
            P[2] = new Vector2 (P[2].X + (to.X - P[3].X), P[2].Y + (to.Y - P[3].Y));
            P[3] = to;
        }

        public override void SplitInThirds(out EdgeSegment part0, out EdgeSegment part1, out EdgeSegment part2)
        {
            // … port full C++ splitInThirds logic …
            throw new NotImplementedException("CubicSegment.SplitInThirds()");
        }
    }
}
