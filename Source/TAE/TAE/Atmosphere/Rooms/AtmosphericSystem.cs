using System;
using System.Collections.Generic;
using TAE.AtmosphericFlow;
using TeleCore.Network.Flow.Values;

namespace TAE.Atmosphere.Rooms;

public class AtmosphericSystem
{
    private List<AtmosphericVolume> _volumes;
    private Dictionary<RoomComponent_Atmosphere, AtmosphericVolume> _flowBoxByPart;
    private Dictionary<AtmosphericVolume, List<AtmosInterface>> _connections;
    
    public void Tick()
    {
        foreach (var volume in _volumes)
        {
            volume.PrevStack = volume.Stack;
            _connections[volume].ForEach(c => c.Notify_SetDirty());
        }
        
        foreach (var volume in _volumes)
        {
            foreach (var connection in _connections[volume])
            {
                var flow = connection.NextFlow;
                flow = FlowFunc(connection.From, connection.To, flow);
                connection.NextFlow = ClampFunc(connection.From, connection.To, flow);
                connection.Move = ClampFunc(connection.From, connection.To, flow);
            }
        }

        foreach (var volume in _volumes)
        {
            for (var i = 0; i < _connections[volume].Count; i++)
            {
                var conn = _connections[volume][i];
                //if(conn.ResolvedMove) continue;
                var res = conn.To.RemoveContent(conn.Move);
                volume.AddContent(res);
                //conn.Notify_ResolvedMove();
            
                //TODO: Structify for: _connections[fb][i] = conn;
            }
        }

        foreach (var volume in _volumes)
        {
            double fp = 0;
            double fn = 0;

            foreach (var conn in _connections[volume])
            {
                Add(conn.Move);
            }
            
            volume.FlowRate = Math.Max(fp, fn);
            continue;

            void Add(double f)
            {
                if (f > 0)
                    fp += f;
                else
                    fn -= f;
            }
        }
    }

    public double Friction => 0;
    public double CSquared => 0.03;
    public double DampFriction => 0.01;

    private double FlowFunc(AtmosphericVolume from, AtmosphericVolume to, double f)
    {
        var dp = Pressure(from) - Pressure(to); // pressure differential
        var src = f > 0 ? from : to;
        var dc = Math.Max(0, src.PrevStack.TotalValue - src.TotalValue);
        f += dp * CSquared;
        f *= 1 - Friction;
        f *= 1 - Math.Min(0.5, DampFriction * dc);
        return f;
    }

    private static double Pressure(AtmosphericVolume volume)
    {
        return volume.TotalValue / volume.MaxCapacity * 100d;
    }

    private static double ClampFlow(double content, double flow, double limit)
    {
        if (content <= 0) return 0;

        if (flow >= 0) return flow <= limit ? flow : limit;
        return flow >= -limit ? flow : -limit;
    }

    private double ClampFunc(AtmosphericVolume from, AtmosphericVolume to, double f, bool enforceMinPipe = true, bool enforceMaxPipe = true)
    {
        var d0 = 1d / Math.Max(1, _connections[from].Count);
        var d1 = 1d / Math.Max(1, _connections[to].Count);

        if (enforceMinPipe)
        {
            double c;
            if (f > 0)
            {
                c = from.TotalValue;
                f = ClampFlow(c, f, d0 * c);
            }
            else if (f < 0)
            {
                c = to.TotalValue;
                f = -ClampFlow(c, -f, d1 * c);
            }
        }

        if (enforceMaxPipe)
        {
            double r;
            if (f > 0)
            {
                r = to.MaxCapacity - to.TotalValue;
                f = ClampFlow(r, f, d1 * r);
            }
            else if (f < 0)
            {
                r = from.MaxCapacity - from.TotalValue;
                f = -ClampFlow(r, -f, d0 * r);
            }
        }

        return f;
    }
}