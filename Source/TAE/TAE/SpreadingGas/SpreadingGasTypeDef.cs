using System;
using UnityEngine;
using Verse;

namespace TAE;

public class SpreadingGasTypeDef : Def
{
    private static int _masterID;
    [Unsaved]
    private AtmosphericTransferWorker workerInt;
    [Unsaved] 
    public int IDReference;
    
    public string texPath;
    public ShaderTypeDef shaderType;
    public Color colorMin;
    public Color colorMax;

    public int maxDensityPerCell = 100;
    public int minDissipationDensity = 10;
    public int minSpreadDensity = 2;
    public int dissipationAmount = 1;
    public float spreadViscosity = 0;

    public AtmosphericDef dissipateTo;
    public Type transferWorker = typeof(AtmosphericTransferWorker);
    
    public FloatRange rotationSpeeds = new FloatRange(100,100);
    public FloatRange expireSeconds = new FloatRange(10, 20);
    public float accuracyPenalty;
    public bool blockTurretTracking;
    public bool roofBlocksDissipation = true;

    public int cellsToDissipatePerTick = 8;
    public int cellsToSpreadPerTick = 8;
    
    //
    public Type pawnEffectWorker;
    public Type cellEffectWorker;

    public AtmosphericTransferWorker TransferWorker => workerInt ??= (AtmosphericTransferWorker)Activator.CreateInstance(transferWorker, this);
    
    public override void PostLoad()
    {
        base.PostLoad();
        IDReference = _masterID++;
    }
}