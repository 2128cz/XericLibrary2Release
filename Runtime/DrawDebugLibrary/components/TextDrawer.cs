namespace DrawXXL
{
    using UnityEngine;

    [HelpURL("https://www.symphonygames.net/drawxxldocumentation/index.html")]
    [AddComponentMenu("Xeric Library/DebugDrawLibrary/Text Drawer")]
    [DefaultExecutionOrder(31000)] //negative numers are early, positive numbers are late. Range is till 32000 to both negative and positive direction.
    public class TextDrawer : VisualizerParent
    {
        public enum SizeInterpretation { globalSpace, sizeOfGameobject, sceneViewWindowWidth, gameViewWindowWidth };
        [SerializeField] SizeInterpretation sizeInterpretation = SizeInterpretation.globalSpace;
        float lengthOfSceneviewScreenWidth_atDrawnObjectsPosition = 1.0f;
        float lengthOfGameviewScreenWidth_atDrawnObjectsPosition = 1.0f;
        float biggestAbsGlobalSizeComponentOfTransform = 1.0f;
        [SerializeField] bool sceneViewCameraForSizeDefinitionIsAvailable = false;
        [SerializeField] bool gameViewCameraForSizeDefinitionIsAvailable = false;
        Camera gameviewCameraForDrawing;
        public enum SizeInterpretationInclFallback { relativeToTheSameAsTextSize, absoluteUnits, relativeToGameobjectSize, relativeToTheSceneViewWindowWidth, relativeToTheGameViewWindowWidth };

        [SerializeField] public Color color = DrawBasics.defaultColor;
        [SerializeField] float size = 0.1f;
        [SerializeField] [Range(0.001f, 0.25f)] float size_relToScreen = 0.01f;
        [SerializeField] public bool size_isOutfolded = true; //is only "public" to silence the compiler warning saying that it is "never used". The compiler doesn't know that it is used via serialization.
        [SerializeField] public DrawText.TextAnchorDXXL textAnchor = DrawText.TextAnchorDXXL.LowerLeft;
        [SerializeField] public bool enclosingBox_isOutfolded = false; //is only "public" to silence the compiler warning saying that it is "never used". The compiler doesn't know that it is used via serialization.
        [SerializeField] public DrawBasics.LineStyle enclosingBoxLineStyle = DrawBasics.LineStyle.invisible;
        [SerializeField] public float enclosingBox_lineWidth_relToTextSize = 0.0f;
        [SerializeField] public float enclosingBox_paddingSize_relToTextSize = 0.0f;

        [SerializeField] public bool forceTextEnlargementToThisMinWidth_isOutfolded = false; //is only "public" to silence the compiler warning saying that it is "never used". The compiler doesn't know that it is used via serialization.
        [SerializeField] bool forceTextEnlargementToThisMinWidth = false;
        [SerializeField] float forceTextEnlargementToThisMinWidth_value = 0.05f;
        [SerializeField] [Range(0.003f, 2.0f)] float forceTextEnlargementToThisMinWidth_value_relToScreen = 0.005f; //upper range end is for vertical text in portrait mode screens
        [SerializeField] SizeInterpretationInclFallback forceTextEnlargementToThisMinWidth_interpretation = SizeInterpretationInclFallback.relativeToTheSameAsTextSize;

        [SerializeField] public bool forceRestrictTextSizeToThisMaxTextWidth_isOutfolded = false; //is only "public" to silence the compiler warning saying that it is "never used". The compiler doesn't know that it is used via serialization.
        [SerializeField] bool forceRestrictTextSizeToThisMaxTextWidth = false;
        [SerializeField] float forceRestrictTextSizeToThisMaxTextWidth_value = 1.0f;
        [SerializeField] [Range(0.003f, 2.0f)] float forceRestrictTextSizeToThisMaxTextWidth_value_relToScreen = 0.1f; //upper range end is for vertical text in portrait mode screens
        [SerializeField] SizeInterpretationInclFallback forceRestrictTextSizeToThisMaxTextWidth_interpretation = SizeInterpretationInclFallback.relativeToTheSameAsTextSize;

        [SerializeField] public bool autoLineBreakWidth_isOutfolded = false; //is only "public" to silence the compiler warning saying that it is "never used". The compiler doesn't know that it is used via serialization.
        [SerializeField] bool autoLineBreakWidth = false;
        [SerializeField] float autoLineBreakWidth_value = 5.0f;
        [SerializeField] [Range(0.003f, 2.0f)] float autoLineBreakWidth_value_relToScreen = 0.5f; //upper range end is for vertical text in portrait mode screens
        [SerializeField] SizeInterpretationInclFallback autoLineBreakWidth_interpretation = SizeInterpretationInclFallback.relativeToTheSameAsTextSize;

        [SerializeField] public bool autoFlipToPreventMirrorInverted = true;
        [SerializeField] bool forceTextTo_facingToSceneViewCam = true;
        [SerializeField] bool forceTextTo_facingToGameViewCam = false;

        public override void InitializeValues_onceInComponentLifetime()
        {
            if (text_exclGlobalMarkupTags == null || text_exclGlobalMarkupTags == "")
            {
                text_exclGlobalMarkupTags = "text to draw";
                text_inclGlobalMarkupTags = "text to draw";
            }
            textSection_isOutfolded = true;

            customVector3Configs[0].picker_isOutfolded = false;
            customVector3Configs[0].source = CustomVector3Source.transformsRight;
            customVector3Configs[0].clipboardForManualInput = Vector3.right;
            customVector3Configs[0].vectorInterpretation = VectorInterpretation.globalSpace;

            customVector3Configs[1].picker_isOutfolded = false;
            customVector3Configs[1].source = CustomVector3Source.transformsUp;
            customVector3Configs[1].clipboardForManualInput = Vector3.up;
            customVector3Configs[1].vectorInterpretation = VectorInterpretation.globalSpace;

            customVector3Configs[2].picker_isOutfolded = false;
            customVector3Configs[2].source = CustomVector3Source.transformsRight;
            customVector3Configs[2].clipboardForManualInput = Vector3.right;
            customVector3Configs[2].vectorInterpretation = VectorInterpretation.globalSpace;

            customVector3Configs[3].picker_isOutfolded = false;
            customVector3Configs[3].source = CustomVector3Source.transformsUp;
            customVector3Configs[3].clipboardForManualInput = Vector3.up;
            customVector3Configs[3].vectorInterpretation = VectorInterpretation.globalSpace;
        }

        public override void DrawVisualizedObject()
        {
            CacheSizeScaleFactors("Text Drawer Component");
            float used_size = Get_used_size();
            if (text_inclGlobalMarkupTags != null && text_inclGlobalMarkupTags != "")
            {
                if (UtilitiesDXXL_Math.ApproximatelyZero(used_size) == false)
                {
                    GetScaledTextBlockConstraintValues(out float used_forceTextEnlargementToThisMinWidth_value, out float used_forceRestrictTextSizeToThisMaxTextWidth_value, out float used_autoLineBreakWidth_value);

                    UtilitiesDXXL_DrawBasics.Set_cameraForAutomaticOrientation_reversible(Get_cameraForAutomaticOrientation());
                    UtilitiesDXXL_Text.WriteFramed(text_inclGlobalMarkupTags, GetDrawPos3D_global(), color, used_size, GetTextDirVector(), GetTextUpVector(), textAnchor, enclosingBoxLineStyle, enclosingBox_lineWidth_relToTextSize, enclosingBox_paddingSize_relToTextSize, used_forceTextEnlargementToThisMinWidth_value, used_forceRestrictTextSizeToThisMaxTextWidth_value, used_autoLineBreakWidth_value, autoFlipToPreventMirrorInverted, 0.0f, hiddenByNearerObjects);
                    UtilitiesDXXL_DrawBasics.Reverse_cameraForAutomaticOrientation();
                }
            }
        }

        public void CacheSizeScaleFactors(string componentNameForErrorLog)
        {
            biggestAbsGlobalSizeComponentOfTransform = Get_biggestAbsGlobalSizeComponentOfTransform();

            if (CheckIf_sceneViewCameraIsRequired())
            {
#if UNITY_EDITOR
                sceneViewCameraForSizeDefinitionIsAvailable = (UnityEditor.SceneView.lastActiveSceneView != null);
                if (sceneViewCameraForSizeDefinitionIsAvailable)
                {
                    float distance_ofDrawnObject_toCamera = (Get_used_drawPos3D_global() - UnityEditor.SceneView.lastActiveSceneView.camera.transform.position).magnitude;
                    lengthOfSceneviewScreenWidth_atDrawnObjectsPosition = UtilitiesDXXL_Screenspace.Get_horizExtentOfViewport_at_distanceFromCam(UnityEditor.SceneView.lastActiveSceneView.camera, distance_ofDrawnObject_toCamera);
                }
#else
            sceneViewCameraForSizeDefinitionIsAvailable = false;
#endif
            }

            if (CheckIf_gameViewCameraIsRequired())
            {
                gameViewCameraForSizeDefinitionIsAvailable = UtilitiesDXXL_Screenspace.GetAutomaticCameraForDrawing(out gameviewCameraForDrawing, componentNameForErrorLog, false);
                if (gameViewCameraForSizeDefinitionIsAvailable)
                {
                    float distance_ofDrawnObject_toCamera = (Get_used_drawPos3D_global() - gameviewCameraForDrawing.transform.position).magnitude;
                    lengthOfGameviewScreenWidth_atDrawnObjectsPosition = UtilitiesDXXL_Screenspace.Get_horizExtentOfViewport_at_distanceFromCam(gameviewCameraForDrawing, distance_ofDrawnObject_toCamera);
                }
            }
        }

        bool CheckIf_sceneViewCameraIsRequired()
        {
            return ((sizeInterpretation == SizeInterpretation.sceneViewWindowWidth) || (forceTextEnlargementToThisMinWidth_interpretation == SizeInterpretationInclFallback.relativeToTheSceneViewWindowWidth) || (forceRestrictTextSizeToThisMaxTextWidth_interpretation == SizeInterpretationInclFallback.relativeToTheSceneViewWindowWidth) || (autoLineBreakWidth_interpretation == SizeInterpretationInclFallback.relativeToTheSceneViewWindowWidth));
        }

        bool CheckIf_gameViewCameraIsRequired()
        {
            return ((sizeInterpretation == SizeInterpretation.gameViewWindowWidth) || (forceTextEnlargementToThisMinWidth_interpretation == SizeInterpretationInclFallback.relativeToTheGameViewWindowWidth) || (forceRestrictTextSizeToThisMaxTextWidth_interpretation == SizeInterpretationInclFallback.relativeToTheGameViewWindowWidth) || (autoLineBreakWidth_interpretation == SizeInterpretationInclFallback.relativeToTheGameViewWindowWidth));
        }

        Vector3 GetTextDirVector()
        {
            if (forceTextTo_facingToSceneViewCam || forceTextTo_facingToGameViewCam)
            {
                return Get_customVector3_3_inGlobalSpaceUnits();
            }
            else
            {
                return Get_customVector3_1_inGlobalSpaceUnits();
            }
        }

        Vector3 GetTextUpVector()
        {
            if (forceTextTo_facingToSceneViewCam || forceTextTo_facingToGameViewCam)
            {
                return Get_customVector3_4_inGlobalSpaceUnits();
            }
            else
            {
                return Get_customVector3_2_inGlobalSpaceUnits();
            }
        }

        public float Get_used_size()
        {
            return ScaleInputFloat_accordingToSizeDefinition(size_relToScreen, size);
        }

        float ScaleInputFloat_accordingToSizeDefinition(float inputFloatToScale_versionThatIsRelToScreen, float inputFloatToScale)
        {
            switch (sizeInterpretation)
            {
                case SizeInterpretation.globalSpace:
                    return inputFloatToScale;
                case SizeInterpretation.sizeOfGameobject:
                    return biggestAbsGlobalSizeComponentOfTransform * inputFloatToScale;
                case SizeInterpretation.sceneViewWindowWidth:
                    if (sceneViewCameraForSizeDefinitionIsAvailable)
                    {
                        return lengthOfSceneviewScreenWidth_atDrawnObjectsPosition * inputFloatToScale_versionThatIsRelToScreen;
                    }
                    else
                    {
                        return inputFloatToScale;
                    }
                case SizeInterpretation.gameViewWindowWidth:
                    if (gameViewCameraForSizeDefinitionIsAvailable)
                    {
                        return lengthOfGameviewScreenWidth_atDrawnObjectsPosition * inputFloatToScale_versionThatIsRelToScreen;
                    }
                    else
                    {
                        return inputFloatToScale;
                    }
                default:
                    return inputFloatToScale;
            }
        }

        public void GetScaledTextBlockConstraintValues(out float used_forceTextEnlargementToThisMinWidth_value, out float used_forceRestrictTextSizeToThisMaxTextWidth_value, out float used_autoLineBreakWidth_value)
        {
            used_forceTextEnlargementToThisMinWidth_value = ScaleTextBlockConstraintFloat_accordingToSizeDefinition(forceTextEnlargementToThisMinWidth, forceTextEnlargementToThisMinWidth_interpretation, forceTextEnlargementToThisMinWidth_value_relToScreen, forceTextEnlargementToThisMinWidth_value);
            used_forceRestrictTextSizeToThisMaxTextWidth_value = ScaleTextBlockConstraintFloat_accordingToSizeDefinition(forceRestrictTextSizeToThisMaxTextWidth, forceRestrictTextSizeToThisMaxTextWidth_interpretation, forceRestrictTextSizeToThisMaxTextWidth_value_relToScreen, forceRestrictTextSizeToThisMaxTextWidth_value);
            used_autoLineBreakWidth_value = ScaleTextBlockConstraintFloat_accordingToSizeDefinition(autoLineBreakWidth, autoLineBreakWidth_interpretation, autoLineBreakWidth_value_relToScreen, autoLineBreakWidth_value);
        }

        public float ScaleTextBlockConstraintFloat_accordingToSizeDefinition(bool constraintIsActive, SizeInterpretationInclFallback interpretationOfConstraintValue, float inputConstraintFloatToScale_versionThatIsRelToScreen, float inputConstraintFloatToScale)
        {
            if (constraintIsActive)
            {
                switch (interpretationOfConstraintValue)
                {
                    case SizeInterpretationInclFallback.relativeToTheSameAsTextSize:
                        return ScaleInputFloat_accordingToSizeDefinition(inputConstraintFloatToScale_versionThatIsRelToScreen, inputConstraintFloatToScale);
                    case SizeInterpretationInclFallback.absoluteUnits:
                        return inputConstraintFloatToScale;
                    case SizeInterpretationInclFallback.relativeToGameobjectSize:
                        return biggestAbsGlobalSizeComponentOfTransform * inputConstraintFloatToScale;
                    case SizeInterpretationInclFallback.relativeToTheSceneViewWindowWidth:
                        if (sceneViewCameraForSizeDefinitionIsAvailable)
                        {
                            return lengthOfSceneviewScreenWidth_atDrawnObjectsPosition * inputConstraintFloatToScale_versionThatIsRelToScreen;
                        }
                        else
                        {
                            return inputConstraintFloatToScale;
                        }
                    case SizeInterpretationInclFallback.relativeToTheGameViewWindowWidth:
                        if (gameViewCameraForSizeDefinitionIsAvailable)
                        {
                            return lengthOfGameviewScreenWidth_atDrawnObjectsPosition * inputConstraintFloatToScale_versionThatIsRelToScreen;
                        }
                        else
                        {
                            return inputConstraintFloatToScale;
                        }
                    default:
                        return inputConstraintFloatToScale;
                }
            }
            else
            {
                return 0.0f;
            }
        }

        public virtual float Get_biggestAbsGlobalSizeComponentOfTransform()
        {
            return UtilitiesDXXL_Math.GetBiggestAbsComponent(transform.lossyScale);
        }

        public virtual Vector3 Get_used_drawPos3D_global()
        {
            return GetDrawPos3D_global();
        }

        DrawBasics.CameraForAutomaticOrientation Get_cameraForAutomaticOrientation()
        {
            if (autoFlipToPreventMirrorInverted)
            {
                if (forceTextTo_facingToSceneViewCam)
                {
                    return DrawBasics.CameraForAutomaticOrientation.sceneViewCamera;
                }
                else
                {
                    if (forceTextTo_facingToGameViewCam)
                    {
                        return DrawBasics.CameraForAutomaticOrientation.gameViewCamera;
                    }
                    else
                    {
                        if (CheckIf_textDirVectorSource_isOrientedAtObserverCam())
                        {
                            return customVector3Configs[0].observerCamera;
                        }
                        else
                        {
                            if (CheckIf_textUpVectorSource_isOrientedAtObserverCam())
                            {
                                return customVector3Configs[1].observerCamera;
                            }
                            else
                            {
                                //= no change
                                return DrawBasics.cameraForAutomaticOrientation;
                            }
                        }
                    }
                }
            }
            else
            {
                //= no change
                return DrawBasics.cameraForAutomaticOrientation;
            }
        }

        bool CheckIf_textDirVectorSource_isOrientedAtObserverCam()
        {
            return ((customVector3Configs[0].source == CustomVector3Source.observerCameraForward) || (customVector3Configs[0].source == CustomVector3Source.observerCameraUp) || (customVector3Configs[0].source == CustomVector3Source.observerCameraRight) || (customVector3Configs[0].source == CustomVector3Source.observerCameraBack) || (customVector3Configs[0].source == CustomVector3Source.observerCameraDown) || (customVector3Configs[0].source == CustomVector3Source.observerCameraLeft) || (customVector3Configs[0].source == CustomVector3Source.observerCameraToThisGameobject));
        }

        bool CheckIf_textUpVectorSource_isOrientedAtObserverCam()
        {
            return ((customVector3Configs[1].source == CustomVector3Source.observerCameraForward) || (customVector3Configs[1].source == CustomVector3Source.observerCameraUp) || (customVector3Configs[1].source == CustomVector3Source.observerCameraRight) || (customVector3Configs[1].source == CustomVector3Source.observerCameraBack) || (customVector3Configs[1].source == CustomVector3Source.observerCameraDown) || (customVector3Configs[1].source == CustomVector3Source.observerCameraLeft) || (customVector3Configs[1].source == CustomVector3Source.observerCameraToThisGameobject));
        }

    }

}
