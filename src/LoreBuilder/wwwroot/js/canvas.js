// Converts a viewport-relative (clientX/clientY) point into a point relative to the given
// scrollable element's own content, accounting for its current scroll position - used to place
// a newly-created lore cluster where a card was actually dropped on the canvas.
window.loreBuilderCanvas = {
    // Returns a plain [x, y] array rather than an object - System.Text.Json (as used
    // internally by Blazor's JS interop) can deserialize an array straight into a .NET array
    // with no constructor-resolution involved, unlike a custom object shape.
    toContentRelative: function (element, clientX, clientY) {
        const rect = element.getBoundingClientRect();
        return [
            clientX - rect.left + element.scrollLeft,
            clientY - rect.top + element.scrollTop
        ];
    },

    // Viewport-absolute center point of element, as [x, y] - used so a button-triggered zoom (no
    // cursor position available) anchors on the same "zoom toward a screen point" logic as
    // scroll-wheel zoom.
    getCenter: function (element) {
        const rect = element.getBoundingClientRect();
        return [rect.left + rect.width / 2, rect.top + rect.height / 2];
    },

    // Adjusts element's scroll position so the canvas-space point currently under
    // (clientX, clientY) stays under it after zoom changes from oldZoom to newZoom - must only
    // be called once the DOM already reflects newZoom's CSS transform (see Home.fs's
    // OnAfterRenderAsync), otherwise the browser clamps the new scrollLeft/Top to the old
    // (pre-zoom) scrollable range.
    zoomAt: function (element, clientX, clientY, oldZoom, newZoom) {
        const rect = element.getBoundingClientRect();
        const cx = clientX - rect.left;
        const cy = clientY - rect.top;
        const contentX = cx + element.scrollLeft;
        const contentY = cy + element.scrollTop;
        const ratio = newZoom / oldZoom;
        element.scrollLeft = contentX * ratio - cx;
        element.scrollTop = contentY * ratio - cy;
    },

    // Scrolls element so canvas-space point (canvasX, canvasY) - already scaled by zoom into
    // element's own content-pixel space - ends up centered in its viewport. Used to center a
    // freshly-shown sub-canvas on its own lone cluster (see Canvas.fs's OnAfterRenderAsync),
    // since native scroll position isn't otherwise preserved when a canvas is hidden/shown again.
    centerOn: function (element, canvasX, canvasY, zoom) {
        const scaledX = canvasX * zoom;
        const scaledY = canvasY * zoom;
        element.scrollLeft = scaledX - element.clientWidth / 2;
        element.scrollTop = scaledY - element.clientHeight / 2;
    },

    // element's own visible viewport size (excluding scrollbars), as [width, height] - used to
    // size the empty-root background dropzone to exactly what's currently visible (see
    // Canvas.fs's OnAfterRenderAsync), so a card can be dropped anywhere on screen rather than
    // only within some fixed-size corner.
    getViewportSize: function (element) {
        return [element.clientWidth, element.clientHeight];
    },

    // Registered directly (not through Bolero's on.wheel/on.preventDefault) because Blazor's own
    // event dispatch round-trip is too slow to reliably beat the browser's native, synchronous
    // Ctrl+wheel page-zoom handling - only a listener registered here, with {passive: false},
    // calling preventDefault() synchronously before Blazor is even involved, actually stops it.
    // Only the Ctrl case calls preventDefault, so plain wheel scrolling is left completely alone.
    registerWheelZoom: function (element, dotNetHelper) {
        element.addEventListener('wheel', function (e) {
            if (e.ctrlKey) {
                e.preventDefault();
                dotNetHelper.invokeMethodAsync('OnCanvasWheelZoom', e.deltaY, e.clientX, e.clientY);
            }
        }, { passive: false });
    },

    // Registered once, window-level, so Escape steps the breadcrumb back one level regardless of
    // which canvas element is currently mounted - same registration lifecycle as
    // registerWheelZoom, just scoped to window instead of one element.
    registerEscapeKey: function (dotNetHelper) {
        window.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                dotNetHelper.invokeMethodAsync('OnEscapePressed');
            }
        });
    }
};
