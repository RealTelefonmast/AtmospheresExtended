using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TAE;

public class SpreadingGasTypeDef : Def
{
    private static ushort _masterID;
    private static readonly Dictionary<int, SpreadingGasTypeDef> _defByID = new();
    
    [Unsaved]
    private AtmosphericTransferWorker workerInt;
    [Unsaved] 
    public ushort IDReference;
    
    //public string texPath;
    //public ShaderTypeDef shaderType;
    public Color colorMin;
    public Color colorMax;

    public int maxDensityPerCell = 100;
    public int minDissipationDensity = 10;
    public int minSpreadDensity = 2;
    public int dissipationAmount = 1;
    public float spreadViscosity = 0;

    public AtmosphericDef dissipateTo;
    public Type transferWorker = typeof(AtmosphericTransferWorker);

    public FloatRange rotationSpeeds = new FloatRange(-100,100);
    public float accuracyPenalty;
    public bool blockTurretTracking;
    public bool roofBlocksDissipation = true;

    public int cellsToDissipatePerTick = 8;
    public int cellsToSpreadPerTick = 8;
    
    //
    public Type pawnEffectWorker;
    public Type cellEffectWorker;

    public AtmosphericTransferWorker TransferWorker => workerInt ??= (AtmosphericTransferWorker)Activator.CreateInstance(transferWorker, this);
    public float ViscosityMultiplier { get; private set; }

    public static implicit operator ushort(SpreadingGasTypeDef def) => def.IDReference;
    public static explicit operator SpreadingGasTypeDef(int ID) => _defByID[ID];

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
        {
            yield return error;
        }

        if (maxDensityPerCell > ushort.MaxValue)
        {
            yield return $"{nameof(maxDensityPerCell)} cannot be larger than {ushort.MaxValue}!";
        }
    }

    public override void PostLoad()
    {
        base.PostLoad();
        IDReference = _masterID++;
        _defByID.Add(IDReference, this);
     
        //
        ViscosityMultiplier = Mathf.Lerp(1, 0.0125f, spreadViscosity);
    }
}