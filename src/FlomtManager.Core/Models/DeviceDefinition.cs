using MemoryPack;

namespace FlomtManager.Core.Models;

[MemoryPackable]
public partial record struct DeviceDefinition(
    int Id,
    DateTime Created,
    DateTime Updated,
    int DeviceId,

    ushort ParameterDefinitionStart,
    ushort ParameterDefinitionNumber,

    ushort DescriptionStart,
    ushort ProgramVersionStart,

    ushort CurrentParameterLineDefinitionStart,
    byte[] CurrentParameterLineDefinition, 
    byte CurrentParameterLineLength, 
    byte CurrentParameterLineNumber, 
    ushort CurrentParameterLineStart, 

    ushort IntegralParameterLineDefinitionStart, 
    byte[] IntegralParameterLineDefinition, 
    byte IntegralParameterLineLength, 
    byte IntegralParameterLineNumber, 
    ushort IntegralParameterLineStart, 

    ushort AverageParameterArchiveLineDefinitionStart, 
    byte[] AverageParameterArchiveLineDefinition, 
    byte AverageParameterArchiveLineLength, 
    byte AverageParameterArchiveLineNumber, 

    ushort AveragePerHourBlockStart, 
    ushort AveragePerHourBlockLineCount, 

    ushort CRC, 

    DateTime? LastArchiveRead);
