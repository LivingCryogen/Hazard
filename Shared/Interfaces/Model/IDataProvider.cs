﻿using Shared.Services.Registry;
using System.Collections.ObjectModel;

namespace Shared.Interfaces.Model;
/// <summary>
/// Reads and converts data from data files.
/// </summary>
/// <remarks>
/// Provides data to <see cref="IAssetFactory"/>, enabling instance construction of <see cref="Type"/>s registered by a <see cref="TypeRegister"/>.<br/>
/// The <see cref="TypeRegister"/> must contain <see cref="object"/>s designated <see cref="RegistryRelation.DataFileName"/>, <see cref="RegistryRelation.DataConverter"/>,<br/>
/// and possibly <see cref="RegistryRelation.ConvertedDataType"/>. 
/// </remarks>  
public interface IDataProvider
{
    /// <summary>
    /// Gets a map of the names and paths of data files needed for asset generation.
    /// </summary>
    /// <value>
    /// The names of data files as listed by 'appsettings.json.' 
    /// </value>
    /// <remarks>Retrieved during 'AppHost' building and injected via DI.</remarks>
    ReadOnlyDictionary<string, string> DataFileMap { get; }
    /// <summary>
    /// Attempts to get data from source files needed to instantiate keyed <see cref="Type"/>s in the <see cref="TypeRegister"/>.<br/>
    /// </summary>
    /// <param name="typeName">The name <see cref="object"/> marked by <see cref="RegistryRelation.Name"/> for the <see cref="Type"/> to be constructed.</param>
    /// <returns>An instance of the <see cref="object"/> marked <see cref="RegistryRelation.ConvertedDataType"/> for <paramref name="typeName"/> in <see cref="TypeRegister"/>. If unsuccessful, <see langword="null"/>.</returns>
    object? GetData(string typeName);
}