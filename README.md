<small class="info">✒ I apologize that English is not my 1<sup>st</sup> language. So these 🗎s are possibly no warranty, just like this license is so. 🐉</small>

---
# 🆇🅴🅻🅵.MagicaGN00T
---

## Overview
* Import models and animation frames from MagicaVoxel `.vox` files.
* Imported meshes have per-vertex color and UV3 as material based voxel data.

## Focused environments in current versions
* [MagicaVoxel](https://ephtracy.github.io/) 0.99.1a		
* Unity 2018.2.3f1		

## Advanced Features from original [MagicaGN00T on 30 Mar 2017](https://github.com/xelfia/MagicaGN00T/commit/fe8c4ccb6d27084c32c57d68f417feb526f6e43c)
* Added supports for [MagicaVoxel .vox File Format extension](https://github.com/ephtracy/voxel-model/blob/master/MagicaVoxel-file-format-vox-extension.txt) (of MagicaVoxel 0.99.1a)
* Added supports for LOD (Level of Detail): simple LOD generation.
* Added supports for material categorized meshes: Opaque / Transparent
* Added supports for Scene Graph (`nTRN` / `nGRP` / `nSHP` chunks)
* Added supports for `MATL` (newer Material) chunks
  * Added shader supports for Smoothess, Emission, Metallic
  * Added Tranparent shader
* Added supports for `LAYR` (Layer) chunks
  * In Scene Graph, objects will import as inactive when its layer is set invisible.
* Added supports for `rOBJ` (Rendering Setting undocumented) chunks
  * note: Not used in imported models
  * Added 'Omits UV unwrapping' option: for fast iteration

## ⚠ Warnings / Limitations
* Scene Graph
  * ⚠ scale origin should be set 0.5 0.5 0.5. Otherwise, incorrect results.
  * ⚠ Some rotations (flips) have bugs.
  * ⚠ Multiple voxel objects will not merged. That is, arrayed tranparent (glass) objects can still be seen visible seams.
* ⚠ This edition written in C#6
  * Required settings: `Player Settings`►`Configuration`►`Scripting Runtime Version` ➡ `.NET 4.x Equivalent`
* ⚠ Breaking Changes contained in.
  * ⚠ Imported assets are incompatible with the original MagicaGN00T. That is, `Generate Model` needed again when you migrate assets.
* ⚠ Colors and materials are imported as per-vertex data. Therefore, original Unity Standard shader not suitable for this.

## Example Included
* ⚠ Not included examples for advanced features
* ⚠ Image is original edition

![Example Image](http://i.imgur.com/hGb84Dt.gif)

### Wireframe
* ⚠ Image is original edition

![Example Wireframe](http://i.imgur.com/mtUNBTO.png)

### Stores frames in drawers  
* ⚠ Image is original edition

![Frames in Drawers](http://i.imgur.com/k64ZOU2.png)

### Baked Lighting (placeholder)
* ⚠ Does not currently cared
* ⚠ Image is original edition

![Baked Imaged](http://i.imgur.com/GiT6omY.png)  
