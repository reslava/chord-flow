---
type: chat
id: ch_01KTMF9BEC6TFWV9QY3K2AJZMR
title: exercises-definition-ui Chat
status: active
created: 2026-06-08
version: 1
tags: []
parent_id: null
requires_load: [rf_01KTM41K36DYJ0CE44FE7TMCGH]
---
# exercises-definition-ui Chat

## Rafa:

### src

Why you pur codebase in `src/ChordFlow.App/` instead of `src/`?
Actually there is no UI layer in src, suggestions?

### Current Exercise implementation

Domain:
`Exercise(Key, Progression, RhythmPattern Rhythm, int Tempo, Difficulty, Feel = Straight)`

Infrastructure:
And here is `ExerciseEntity`: `src/ChordFlow.App/Infrastructure/Entities/ExerciseEntity.cs` 

### What should be Exercise definition

To create an exercise we need a simple UI that let users indicate something like:

1. harmony + rhythm
2. target notes + lead rhythm

`ExerciseEntity` should be:
- `Progression`
- `RhythmPattern` for rhythm 
- `RhythmPattern` for lead (optional)
- `Target notes`

`Target notes` should be related to harmony (chords progression), could be:
- a scale for the key
- chord related:
  - a scale for each chord
  - chord tones

We should implement an easy, clear UI to let the user define a new or pick an availables:
- `Progression` 
- and 2 `RhythmPattern`s 
- `Target notes`.

- Which UI could we user for define a `RhythmPattern`?


Other params of Exercise will be selectable in the app UI and generate the alplaTex when Generate button is pressed.

### Exercise params

When we save an ExerciseEntity in database only save the Exercise definition, the new ExerciseEntity

- Key, 
- int Tempo
- Difficulty
- Feel = Straight

new params:
- count in
- metronome activated
- Rhythmic guitar volume
- Lead guitar volume

### Practice / Play an Exercise

- Tablature could have 2 tracks 
  - 1. Rhythmic guitar: chord diagrams showed when chord change
  - 2. Lead guitar: depending if `RhythmPattern` for lead is defined
  
### `Target notes`

We will postpone this for now.
In actual version we will play dead notes in `RhythmPattern` lead grid pattern  