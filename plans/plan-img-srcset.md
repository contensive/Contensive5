# Proposal: Responsive Image Methods for `cp.Image`

## The Problem

Every widget that displays images (CarouselWidget, HeroImageWidget, ImageGalleryWidget, etc.) currently renders a single `<img src="...">` tag with one fixed-size image. The browser downloads that one image regardless of the user's viewport, device pixel ratio, or container size. There's no `srcset`, `sizes`, or `<picture>` support anywhere in the platform.

Meanwhile, the resize infrastructure is already mature -- `cp.Image.ResizeAndCrop` generates multiple sizes on demand, stores them alongside the original, and tracks them in `imageAltSizeList`. The missing piece is a method that **orchestrates multiple resize calls and returns the HTML attributes (or full markup)** needed for responsive delivery.

## Proposed New Methods on `CPImageBaseClass`

Two new methods, each solving a different use case:

### 1. `GetImgSrcSet` -- for `<img srcset="" sizes="" src="">`

Best for: most image use cases (carousel slides, gallery thumbnails, content images) where you have a single image that should be served at different sizes based on viewport width.

```csharp
/// <summary>
/// Generate srcset, sizes, and src attribute values for a responsive img tag.
/// Creates multiple resized variants and returns a model with the attribute values.
/// The caller renders these into a mustache template.
/// </summary>
/// <param name="imagePathFilename">CDN path to the original image</param>
/// <param name="holeWidth">The max display width in px (used as largest srcset size)</param>
/// <param name="holeHeight">The display height in px (0 = auto/proportional)</param>
/// <param name="imageAltSizes">The alt size list to read/update (pass by ref, save if isNewSize)</param>
/// <param name="isNewSize">True if new sizes were generated (caller must save imageAltSizes)</param>
/// <param name="sizes">Optional sizes attribute value. If empty, defaults to "(max-width: {holeWidth}px) 100vw, {holeWidth}px"</param>
/// <returns>ImgSrcSetResult with src, srcset, and sizes strings</returns>
public abstract ImgSrcSetResult GetImgSrcSet(
    string imagePathFilename,
    int holeWidth,
    int holeHeight,
    ref string imageAltSizes,
    out bool isNewSize,
    string sizes = "");
```

Returns a simple result object:

```csharp
public class ImgSrcSetResult {
    /// <summary>Fallback src (largest size, for browsers that don't support srcset)</summary>
    public string src { get; set; }
    /// <summary>srcset attribute value, e.g. "image-400x300.webp 400w, image-800x600.webp 800w, image-1200x900.webp 1200w"</summary>
    public string srcset { get; set; }
    /// <summary>sizes attribute value, e.g. "(max-width: 1200px) 100vw, 1200px"</summary>
    public string sizes { get; set; }
}
```

**What it does internally:**
1. Takes the `holeWidth` as the maximum display width.
2. Generates a standard set of breakpoint widths that are smaller than `holeWidth`. For example, if `holeWidth = 1200`, it might generate sizes at 400, 800, and 1200 pixels wide (each with proportional height based on `holeHeight`, or the original aspect ratio if `holeHeight = 0`).
3. Calls `ResizeAndCrop` (or `ResizeAndPad`) for each breakpoint, passing through the `imageAltSizes` ref so all variants are tracked.
4. Builds the `srcset` string with `w` descriptors.
5. If no explicit `sizes` is passed, generates a sensible default.

### 2. `GetPictureSource` -- for `<picture><source media="..." srcset="...">`

Best for: hero images and cases where you want **different crops or aspect ratios** at different breakpoints (art direction), or where you want to serve WebP to supporting browsers with a format fallback.

```csharp
/// <summary>
/// Generate source elements and fallback img attributes for a picture element.
/// Creates multiple resized variants at different breakpoints with optional different aspect ratios.
/// </summary>
/// <param name="imagePathFilename">CDN path to the original image</param>
/// <param name="breakpoints">List of breakpoint definitions (min-width, holeWidth, holeHeight)</param>
/// <param name="imageAltSizes">The alt size list to read/update</param>
/// <param name="isNewSize">True if new sizes were generated</param>
/// <returns>PictureResult with list of sources and fallback img attributes</returns>
public abstract PictureResult GetPictureSource(
    string imagePathFilename,
    List<PictureBreakpoint> breakpoints,
    ref string imageAltSizes,
    out bool isNewSize);
```

Supporting types:

```csharp
public class PictureBreakpoint {
    /// <summary>min-width media query value in px (e.g. 1200, 768, 0)</summary>
    public int minWidth { get; set; }
    /// <summary>Image width to generate for this breakpoint</summary>
    public int holeWidth { get; set; }
    /// <summary>Image height to generate (0 = proportional)</summary>
    public int holeHeight { get; set; }
}

public class PictureResult {
    /// <summary>List of source elements, ordered largest-first for correct media query matching</summary>
    public List<PictureSourceResult> sources { get; set; }
    /// <summary>Fallback src for the img element (smallest breakpoint)</summary>
    public string fallbackSrc { get; set; }
}

public class PictureSourceResult {
    /// <summary>media attribute value, e.g. "(min-width: 1200px)"</summary>
    public string media { get; set; }
    /// <summary>srcset attribute value (the resized image URL)</summary>
    public string srcset { get; set; }
}
```

## How This Integrates with Mustache Layouts

The key design decision: these methods return **data**, not HTML. The view model populates mustache variables, and the layout controls the markup. This preserves the existing separation of concerns.

### Example: CarouselWidget with `GetImgSrcSet`

**View model change** (in `CarouselWidgetViewModel.addImage`):

```csharp
// Instead of:
//   resultImage.imageSrc = ImageController.resizeImage(cp, imagePathFilename, ...);
// Do:
var imgResult = cp.Image.GetImgSrcSet(
    imagePathFilename,
    (int)settings.imageWidth,
    imageHeight,
    ref imageAltSizeList,
    out bool isNewSize);
resultImage.imageSrc = imgResult.src;
resultImage.imageSrcSet = imgResult.srcset;
resultImage.imageSizes = imgResult.sizes;
```

**Layout change** (in `CarouselWidgetLayout.html`):

```html
<!-- Before -->
<img class="designBlockImage" src="{{imageSrc}}" alt="{{imageAlt}}">

<!-- After -->
<img class="designBlockImage"
     src="{{imageSrc}}"
     srcset="{{imageSrcSet}}"
     sizes="{{imageSizes}}"
     alt="{{imageAlt}}"
     loading="lazy">
```

The mustache approach works naturally -- `srcset` and `sizes` are just string attributes. If either is empty (e.g., no resize configured), the attribute renders empty and the browser ignores it, falling back to `src`.

### Example: HeroImageWidget with `GetPictureSource`

The hero image currently uses CSS `background-image`. To use `<picture>`, the layout would switch to using an `<img>` element (which can be positioned absolutely and styled the same way). The view model would populate a list of sources:

**View model:**

```csharp
var breakpoints = new List<PictureBreakpoint> {
    new PictureBreakpoint { minWidth = 1200, holeWidth = 1920, holeHeight = 600 },
    new PictureBreakpoint { minWidth = 768, holeWidth = 1200, holeHeight = 500 },
    new PictureBreakpoint { minWidth = 0, holeWidth = 768, holeHeight = 400 }
};
var pictureResult = cp.Image.GetPictureSource(
    settings.backgroundImageFilename, breakpoints, ref altSizes, out bool isNewSize);
result.pictureSources = pictureResult.sources; // list for mustache section
result.fallbackSrc = pictureResult.fallbackSrc;
```

**Layout:**

```html
<div class="blockHero" style="{{styleHeight}}">
  <picture>
    {{#pictureSources}}
    <source media="{{media}}" srcset="{{srcset}}">
    {{/pictureSources}}
    <img src="{{fallbackSrc}}" alt="{{imageAlt}}"
         class="blockHeroImage" loading="lazy">
  </picture>
  <div class="blockHeroShade {{shadeClass}}"></div>
  <!-- text overlay unchanged -->
</div>
```

The `<picture>` element's `<source>` list is a natural fit for a mustache section loop.

## Breakpoint Size Selection Strategy

The internal logic for `GetImgSrcSet` should generate a practical set of widths. A reasonable default:

| holeWidth range | Generated widths |
|---|---|
| <= 400 | holeWidth only (single image, too small to benefit) |
| 401-800 | holeWidth/2, holeWidth |
| 801-1600 | 400, holeWidth/2, holeWidth |
| > 1600 | 400, 800, 1200, holeWidth |

This keeps the number of resize operations bounded (max 4 variants) while covering the common viewport breakpoints. The `imageAltSizeList` caching means these are only generated once per image.

## WebP and Format Considerations

The existing `ResizeAndCrop` already converts to WebP. The `<picture>` element offers a natural place for format fallbacks:

```html
<picture>
  <source type="image/webp" srcset="{{webpSrcSet}}" sizes="{{imageSizes}}">
  <img src="{{jpgSrc}}" srcset="{{jpgSrcSet}}" sizes="{{imageSizes}}" alt="...">
</picture>
```

However, this adds complexity. Since the existing codebase already converts everything to WebP via `ResizeAndCrop`, and WebP browser support is now essentially universal (97%+), starting with `GetImgSrcSet` returning WebP URLs only is recommended. Format fallback can be added as a future enhancement if needed.

## What Changes Where

**Contensive5 (this repo):**
- `CPImageBaseClass` -- add abstract methods `GetImgSrcSet`, `GetPictureSource` and the result classes
- `CPImageClass` -- implement them using the existing `ImageController.resize` infrastructure
- `ImageController` -- no changes needed (the new methods call `resize` multiple times)

**aoDesignBlocks:**
- `CarouselWidgetImageViewModel` -- add `imageSrcSet`, `imageSizes` properties
- `CarouselWidgetViewModel.addImage` -- call `GetImgSrcSet` instead of `resizeImage`
- `CarouselWidgetLayout.html` -- add `srcset` and `sizes` to `<img>` tags
- `ImageGalleryWidgetViewModel` -- same pattern for gallery images
- `ImageGalleryLayout_BS5.html` -- add responsive attributes
- `HeroImageWidgetViewModel` -- use `GetPictureSource` for art direction
- `HeroImageLayout_BS5.html` -- switch from CSS background-image to `<picture>` element

## Migration Path

Both methods are additive. Existing widgets continue to work unchanged. Individual widgets can adopt responsive images one at a time by:
1. Updating their view model to call the new method
2. Adding `srcset`/`sizes` (or `<picture>/<source>`) to their layout HTML
3. No database schema changes needed -- the existing `imageAltSizeList` field already handles the additional sizes
