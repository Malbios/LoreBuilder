// Measures the real, currently-rendered height of a Card's active Complex cue content div, so
// Card.fs can size the card's own growth (see CardHelpers.growthEdge) and the rotated left/right
// edge's position to the content's actual height instead of a hand-picked constant.
window.loreBuilderCue = {
    // offsetHeight, not scrollHeight: .complex-cue has no fixed height or overflow constraint
    // (see Card.bolero.css - height:auto/min-height:50px), so the box's own layout height
    // already is its content's natural height. offsetHeight also ignores the div's own
    // `transform: rotate(...)` (transforms never affect the layout box), which is exactly what's
    // needed for the left/right case.
    measureHeight: function (element) {
        return element.offsetHeight;
    }
};
