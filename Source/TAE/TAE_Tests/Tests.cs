#define IS_TELE_DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TAE;
using TAE.Atmosphere.Rooms;
using TAE.AtmosphericFlow;
using TeleCore.Network.Data;
using TeleCore.Primitive;
using UnityEngine;

namespace TAE_Tests
{
    [TestFixture]
    public class EqualizationTests
    {
        private static List<AtmosphericVolume> volumes;
        private static List<AtmosInterface> interfaces;
        private static Dictionary<AtmosphericVolume, List<AtmosInterface>> connections;

        public static AtmosphericDef[] defs = new AtmosphericDef[2]
        {
            new AtmosphericDef
            {
                defName = "GasA",
                label = "gas A",
                labelShort = "a",
                valueUnit = "°",
                valueColor = Color.red,
                viscosity = 1,
                friction = 0
            },
            new AtmosphericDef
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
            var config = new FlowVolumeConfig<AtmosphericDef>
            {
                allowedValues = new List<AtmosphericDef>(),
                capacity = 1000
            };

            volumes = new List<AtmosphericVolume>();
            connections = new Dictionary<AtmosphericVolume, List<AtmosInterface>>();
            volumes.Add(new AtmosphericVolume(config));
            volumes.Add(new AtmosphericVolume(config));
            volumes[0].UpdateVolume(10);
            volumes[1].UpdateVolume(10);

            var atmosInterface = new List<AtmosInterface> {new(volumes[0], volumes[1])};
            connections.Add(volumes[0], atmosInterface);
            connections.Add(volumes[1], atmosInterface);

            interfaces = new List<AtmosInterface>(atmosInterface);
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
        
        private static void Equalize(int step)
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
                flow = AtmosphericSystem.FlowFunc(from, to, flow, out double dp);
                conn.UpdateBasedOnFlow(flow);
                flow = Math.Abs(flow);
                conn.NextFlow = AtmosphericSystem.ClampFunc(connections,from, to, flow);
                conn.Move = AtmosphericSystem.ClampFunc(connections, from, to, flow);
            }
            
            //Upate Content
            foreach (var conn in interfaces)
            {
                DefValueStack<AtmosphericDef, double> res = conn.From.RemoveContent(conn.Move);
                conn.To.AddContent(res);
                //Console.WriteLine($"Moved: " + conn.Move + $":\n{res}");
                    
                //TODO: Structify for: _connections[fb][i] = conn;
            }
        }
    }
}