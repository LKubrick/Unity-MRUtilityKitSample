This package is compatible with The Built-in Render Pipeline by default. For compatibility with URP or HDRP, please, import the corresponding unitypackage, included in the project. 



If you want to use this assets in HDRP project install the HDRP Assets.unitypackage from Render Pipelines folder.

In order to run Art Deco Demoscene with proper settings you might need to follow next steps:

- Go to Edit > Project Settings > Graphics. In the Scriptable Render Pipeline Settings field assign ArtDecoHDRenderPipelineAsset.

- Go to Edit > Project Settings > Player > Other Settings > Rendering > Color Space and change the value from Gamma to Linear. 

- Go to Edit > Project Settings > HDRP Default Settings and change the Default Volume Profile Asset to Art Deco Demoscene Volume Profile.




If you want to use this assets in URP project install the URP Assets.unitypackage from Render Pipelines folder.


In order to run Art Deco Demoscene with proper settings you might need to follow next steps:

- Go to Edit > Project Settings > Graphics. In the Scriptable Render Pipeline Settings field assign ArtDecoUniversalRenderPipelineAsset.

- Go to Edit > Project Settings > Player > Other Settings > Rendering > Color Space and change the value from Gamma to Linear. 

- In newer Unity versions also check for Post Processing in ArtDecoUniversalRenderPipelineAsset_Renderer.asset if Post Processing turned off.