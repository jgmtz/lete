using System.Text.Json.Nodes;
using Lete.Core.Combat;
using Lete.Core.State;

namespace Lete.Core.Tests;

public class SaveGameTests
{
    private static WorldState Filled()
    {
        var state = new WorldState
        {
            Gender = Gender.Feminine,
            ProtagonistId = "sombra_f",
            OriginId = "veterano",
            CompanionId = "aulo",
            Mark = 5,
            MarkStage = 2,
            FinalFace = FinalFace.Covered,
            Tide = 2.3m,
            RegionId = "sala_teste",
            Position = new System.Numerics.Vector3(1.5f, 0f, -2.25f),
        };
        state.HeavyMemories["venda_do_nome"] = HeavyMemoryChoice.Leave;
        state.HeavyMemories["o_irmao"] = HeavyMemoryChoice.Face;
        state.TideMilestones.Add("aqueronte");
        state.Recollections["agua_parada"] = new RecollectionProgress { FullAct = "ancora_cais" };
        state.Recollections["agua_parada"].Sources.UnionWith(new[] { "remador", "pescador_velho" });
        state.Recollections["mentira"] = new RecollectionProgress { False = true };
        state.OriginFragments.Add("veterano_escudo");
        state.JournalEntries.Add("obj.anfora.examinar");
        state.JournalEntries.Add("obj.estela.examinar");
        state.Flags.Add("viu_o_rio");
        state.PathKeys["aqueronte"] = "obolo";
        state.Items.Owned["remo_do_afogado"] = 1;
        state.Items.Owned["moeda_do_afogado"] = 0;
        state.Items.Equipped[Lete.Core.Items.EquipSlot.Instrument] = "remo_do_afogado";
        state.Items.Equipped[Lete.Core.Items.EquipSlot.Votive2] = "moeda_do_afogado";
        state.Items.CompletedSets.Add("conjunto_do_afogado");
        state.Inventory.Obols = 7;
        state.Inventory.Offerings["mel"] = 2;
        state.Inventory.Provisions["pao"] = 1;
        state.Vitals.BreathSpent = 12.5f;
        state.Vitals.LuciditySpent = 4f;
        state.Vitals.HealUses["fogo_asfodelos"] = 2;
        state.Dialogue.Chosen.Add("padeiro/dlg.padeiro.vago.escutar");
        state.Dialogue.CommentsSeen.Add("padeiro_mulher");
        state.Dialogue.Awakened.Add("padeiro");
        state.Oaths["mao_aberta"] = OathStatus.Broken;
        state.Oaths["do_lar"] = OathStatus.Active;
        state.Oaths["do_silencio"] = OathStatus.Kept;
        state.Profile.ViaTotals[Via.Free] = 3;
        state.Profile.KindTotals[RecollectionKind.Mourning] = 2;
        state.Profile.Recent.Add(new ProfileEncounter(Via.Free, new[] { RecollectionKind.Mourning, RecollectionKind.Mourning }));
        state.Profile.Recent.Add(new ProfileEncounter(null, Array.Empty<RecollectionKind>()));
        return state;
    }

    [Fact]
    public void RoundTrip_KeepsEveryField()
    {
        var original = Filled();
        var loaded = SaveGame.Read(SaveGame.Write(original));

        Assert.Equal(original.Gender, loaded.Gender);
        Assert.Equal(original.ProtagonistId, loaded.ProtagonistId);
        Assert.Equal(original.OriginId, loaded.OriginId);
        Assert.Equal(original.CompanionId, loaded.CompanionId);
        Assert.Equal(original.Mark, loaded.Mark);
        Assert.Equal(original.MarkStage, loaded.MarkStage);
        Assert.Equal(original.FinalFace, loaded.FinalFace);
        Assert.Equal(original.Tide, loaded.Tide);
        Assert.Equal(original.TideMilestones, loaded.TideMilestones);
        Assert.Equal(original.Recollections.Keys, loaded.Recollections.Keys);
        Assert.Equal(original.Recollections["agua_parada"].Sources, loaded.Recollections["agua_parada"].Sources);
        Assert.Equal("ancora_cais", loaded.Recollections["agua_parada"].FullAct);
        Assert.True(loaded.Recollections["mentira"].False);
        Assert.Equal(original.RegionId, loaded.RegionId);
        Assert.Equal(original.Position, loaded.Position);
        Assert.Equal(original.HeavyMemories, loaded.HeavyMemories);
        Assert.Equal(original.OriginFragments, loaded.OriginFragments);
        Assert.Equal(original.JournalEntries, loaded.JournalEntries);
        Assert.Equal(original.Flags, loaded.Flags);
        Assert.Equal(original.PathKeys, loaded.PathKeys);
        Assert.Equal(original.Items.Owned, loaded.Items.Owned);
        Assert.Equal(original.Items.Equipped, loaded.Items.Equipped);
        Assert.Equal(original.Items.CompletedSets, loaded.Items.CompletedSets);
        Assert.Equal(7, loaded.Inventory.Obols);
        Assert.Equal(original.Inventory.Offerings, loaded.Inventory.Offerings);
        Assert.Equal(original.Inventory.Provisions, loaded.Inventory.Provisions);
        Assert.Equal((12.5f, 4f), (loaded.Vitals.BreathSpent, loaded.Vitals.LuciditySpent));
        Assert.Equal(original.Vitals.HealUses, loaded.Vitals.HealUses);
        Assert.Equal(original.Dialogue.Chosen, loaded.Dialogue.Chosen);
        Assert.Equal(original.Dialogue.CommentsSeen, loaded.Dialogue.CommentsSeen);
        Assert.Equal(original.Dialogue.Awakened, loaded.Dialogue.Awakened);
        Assert.Equal(original.Oaths, loaded.Oaths);
        Assert.Equal(original.Profile.ViaTotals, loaded.Profile.ViaTotals);
        Assert.Equal(original.Profile.KindTotals, loaded.Profile.KindTotals);
        Assert.Equal(
            original.Profile.Recent.Select(e => (e.Via, string.Join(",", e.Kinds))),
            loaded.Profile.Recent.Select(e => (e.Via, string.Join(",", e.Kinds))));
        Assert.Equal(SaveGame.Write(original), SaveGame.Write(loaded));
    }

    [Fact]
    public void RoundTrip_NewGameHasNullChoices()
    {
        var loaded = SaveGame.Read(SaveGame.Write(new WorldState()));

        Assert.Null(loaded.ProtagonistId);
        Assert.Null(loaded.OriginId);
        Assert.Equal(FinalFace.None, loaded.FinalFace);
        Assert.Empty(loaded.HeavyMemories);
        Assert.Null(loaded.RegionId);
        Assert.Null(loaded.Position);
    }

    [Fact]
    public void Read_RejectsMalformedPosition()
    {
        var json = JsonNode.Parse(SaveGame.Write(Filled()))!;
        json["posicao"] = new JsonArray(1f, 2f);
        Assert.Throws<FormatException>(() => SaveGame.Read(json.ToJsonString()));
    }

    [Fact]
    public void Written_IsReadableJsonWithVersion()
    {
        var text = SaveGame.Write(Filled());
        var json = JsonNode.Parse(text)!;

        Assert.Contains('\n', text);
        Assert.Equal(SaveGame.CurrentVersion, json["versao"]!.GetValue<int>());
        Assert.Equal("coberto", json["rosto_final"]!.GetValue<string>());
        Assert.Equal("deixou", json["memorias_pesadas"]!["venda_do_nome"]!.GetValue<string>());
    }

    [Fact]
    public void Read_AppliesMigrationsInOrder()
    {
        // Um save da versão 1 lido por um jogo "na versão 3": cada migração roda uma vez, na ordem.
        var order = new List<int>();
        var migrations = new Dictionary<int, Action<JsonObject>>
        {
            [1] = json => { order.Add(1); json["origem"] = "pescador"; },
            [2] = json => { order.Add(2); json["marca"] = json["marca"]!.GetValue<int>() + 1; },
        };

        var loaded = SaveGame.Read(SaveGame.Write(Filled()), migrations, currentVersion: 3);

        Assert.Equal(new[] { 1, 2 }, order);
        Assert.Equal("pescador", loaded.OriginId);
        Assert.Equal(6, loaded.Mark);
    }

    [Fact]
    public void Read_RejectsMissingMigrationAndNewerVersion()
    {
        var text = SaveGame.Write(Filled());

        Assert.Throws<FormatException>(() => SaveGame.Read(text, new Dictionary<int, Action<JsonObject>>(), currentVersion: 2));
        Assert.Throws<FormatException>(() => SaveGame.Read(text.Replace("\"versao\": 1", "\"versao\": 99")));
    }
}
