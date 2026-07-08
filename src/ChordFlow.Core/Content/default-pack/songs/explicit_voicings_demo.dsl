name: Explicit Voicings Demo
genre: Blues
subgenre: Demo
tags: [12-bar, voicings, demo]

# A 12-bar blues that shows every explicit-voicing tier (guitar/explicit-voicing-reference):
#   voice *7      — a movable A-shape dom7 grip for every dominant-7 chord
#   voice #4dim7  — the passing dim7, taken from the engine catalog by reference
#   {…}           — bar 1's I7 is pinned to an E-shape grip; bar 11's I7 is a rootless shell
voice *7 = x 3 2 3 1 x
voice #4dim7 = a: auto:caged:dim7:A

head = 17 {8 10 8 9 8 8} 47 17 17 47 #4dim7 17 17 57 47 17 {x x 2 3 x x root:6@8} 57

key C
head
