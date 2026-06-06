# Indirect mapper lookup

Some ROM hacks keep legacy one-byte IDs in save/party data, but store the real expanded ID in a conversion table elsewhere in RAM.

For example, a pokecrystal16 party Pokémon may store `0xFE` in the normal party species byte, while the real 16-bit species ID lives in a conversion table:

```text
party species byte = 0xFE
conversion table entries base = 0xD202
conversion table entry size = 2
lookup address = 0xD202 + ((0xFE - 1) * 2)
lookup value = 0x0102
glossary pokemonSpecies16[0x0102] = Sceptile
```

Use `indirectAddress` and `indirectSize` to make GameHook perform that table lookup before applying the usual `reference` glossary.

## Fields

```yaml
indirectAddress: 0xD202
indirectSize: 2
indirectIndexOffset: 1
indirectEntryCount: 100
indirectMemoryContainer: WRAM
```

`indirectAddress` is the base address of the conversion table entries, not necessarily the base of the whole table header.

`indirectSize` is the size, in bytes, of each table entry.

`indirectIndexOffset` is optional. It defaults to `0`. The table index is calculated as:

```text
rawValue - indirectIndexOffset
```

If the raw value is below `indirectIndexOffset`, GameHook leaves the original value alone and applies the normal glossary/reference lookup. This lets one property support both vanilla one-byte IDs and compact PC16 tables that only contain expanded slots.

If `indirectEntryCount` is set and the calculated table index is outside the table, GameHook also keeps the raw value. A table lookup that returns `0` is treated as an empty slot and falls back to the raw value.

`indirectMemoryContainer` is optional. If omitted, the property uses the same memory container as the original property.

## Example: pokecrystal16 species

```yaml
player:
  team:
    - species:
        type: "int"
        address: 0xDCD7
        reference: "pokemonSpecies16"
        indirectAddress: 0xD202
        indirectSize: 2
        indirectIndexOffset: 1
        indirectEntryCount: 100

glossary:
  pokemonSpecies16:
    0x0000: null
    0x0001: "Bulbasaur"
    0x0002: "Ivysaur"
    0x0003: "Venusaur"
    # ...
    0x00FB: "Celebi"
    0x0100: "Treecko"
    0x0101: "Grovyle"
    0x0102: "Sceptile"
```

The same pattern can be used for expanded Pokémon, move, or item IDs. The mapper remains responsible for pointing at the correct conversion table for each category.
