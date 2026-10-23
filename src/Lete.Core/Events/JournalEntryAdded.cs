namespace Lete.Core.Events;

/// <summary>Uma frase nova entrou no Diário. <paramref name="EntryKey"/> é uma chave de localização.</summary>
public sealed record JournalEntryAdded(string EntryKey);
