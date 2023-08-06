#define IS_TELE_DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TAE;
using TAE.Atmosphere.Rooms;
using TAE.AtmosphericFlow;
using TeleCore.FlowCore;
using TeleCore.Network.Data;
using TeleCore.Primitive;
using UnityEngine;

namespace TAE_Tests
{
    [TestFixture]
    public class EqualizationTests
    {
        private static List<AtmosphericVolume> volumes;
        private static List<FlowInterface<int,AtmosphericVolume, AtmosphericValueDef>> interfaces;
        private static Dictionary<AtmosphericVolume, List<FlowInterface<int,AtmosphericVolume, AtmosphericValueDef>>> connections;

        public static AtmosphericValueDef[] defs = new AtmosphericValueDef[2]
        {
            new AtmosphericValueDef
            {
                defName = "GasA",
                label = "gas A",
                labelShort = "a",
                valueUnit = "°",
                valueColor = Color.red,
                viscosity = 1,
                friction = 0
            },
            new AtmosphericValueDef
            {
                defName = "GasB",
                label = "gas B",
                labelShort = "b",
                valueUnit = "°",
                valueColor = Color.blue,
                viscosity = 1,
                friction = 0
            },
        };
        
        [SetUp]
        public void Setup()
        {
            var config = new FlowVolumeConfig<AtmosphericValueDef>
            {
                //allowedValues = new List<AtmosphericValueDef>(),
                capacity = 1000
            };

            volumes = new List<AtmosphericVolume>();
            connections = new Dictionary<AtmosphericVolume, List<FlowInterface<AtmosphericVolume, AtmosphericValueDef>>>();
            volumes.Add(new AtmosphericVolume(config));
            volumes.Add(new AtmosphericVolume(config));
            volumes[0].UpdateVolume(10);
            volumes[1].UpdateVolume(10);

            var atmosInterface = new List<FlowInterface<AtmosphericVolume, AtmosphericValueDef>> {new(volumes[0], volumes[1])};
            connections.Add(volumes[0], atmosInterface);
            connections.Add(volumes[1], atmosInterface);

            interfaces = new List<FlowInterface<AtmosphericVolume, AtmosphericValueDef>>(atmosInterface);
        }
        
        [Test]
        public void Equalization()
        {
            var res1 = volumes[0].TryAdd(defs[0], 250);
            var res2 = volumes[0].TryAdd(defs[1], 250);

            int count = 0;
            do
            {
                Equalize(count);
                count++;
                Console.WriteLine($"[{count}][{interfaces[0].NextFlow}]");

            } 
            while (interfaces[0].NextFlow > 0.00001);
                
            var calc = Math.Abs(500d - (volumes[0].TotalValue + volumes[1].TotalValue));
            Assert.IsTrue(calc < 0.00001d);
        }
        
        private void Equalize(int step)
        {
            //Prepare
            foreach (AtmosphericVolume volume in volumes)
            {
                volume.PrevStack = volume.Stack;
            }
            
            //Update Flow
            foreach (var conn in interfaces)
            {
                double flow = conn.NextFlow;      
                var from = conn.From;
                var to = conn.To;
                flow = AtmosphericSystem.FlowFunc(conn, flow);
                conn.UpdateBasedOnFlow(flow);
                flow = Math.Abs(flow);
                conn.NextFlow = AtmosphericSystem.ClampFunc(connections,from, to, flow);
                conn.Move = AtmosphericSystem.ClampFunc(connections, from, to, flow);
            }
            
            //Upate Content
            foreach (var conn in interfaces)
            {
                DefValueStack<AtmosphericValueDef, double> res = conn.From.RemoveContent(conn.Move);
                conn.To.AddContent(res);
                //Console.WriteLine($"Moved: " + conn.Move + $":\n{res}");
                    
                //TODO: Structify for: _connections[fb][i] = conn;
            }
        }
    }
}