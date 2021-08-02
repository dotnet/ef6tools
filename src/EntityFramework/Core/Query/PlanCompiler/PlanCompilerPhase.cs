// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Query.PlanCompiler
{
    // <summary>
    // Enum describing which phase of plan compilation we're currently in
    // </summary>
    internal enum PlanCompilerPhase
    {
        // <summary>
        // Just entering the PreProcessor phase
        // </summary>
        PreProcessor = 0,

        // <summary>
        // Entering the AggregatePushdown phase
        // </summary>
        AggregatePushdown = 1,

        // <summary>
        // Entering the Normalization phase
        // </summary>
        Normalization = 2,

        // <summary>
        // Entering the NTE (Nominal Type Eliminator) phase
        // </summary>
        NTE = 3,

        // <summary>
        // Entering the Projection pruning phase
        // </summary>
        ProjectionPruning = 4,

        // <summary>
        // Entering the Nest Pullup phase
        // </summary>
        NestPullup = 5,

        // <summary>
        // Entering the Transformations phase
        // </summary>
        Transformations = 6,

        // <summary>
        // Entering the JoinElimination phase
        // </summary>
        JoinElimination = 7,

        NullSemantics = 8,

        // <summary>
        // Entering the codegen phase
        // </summary>
        CodeGen = 9,

        // <summary>
        // We're almost done
        // </summary>
        PostCodeGen = 10,

        // <summary>
        // Marker
        // </summary>
        MaxMarker = 11
    }
}
