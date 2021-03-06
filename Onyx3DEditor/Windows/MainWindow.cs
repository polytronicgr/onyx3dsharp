﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

using Onyx3D;
using OpenTK;

namespace Onyx3DEditor
{
    
	public partial class MainWindow : SingletonForm<MainWindow>
	{
   
        bool canDraw = false;

		Onyx3DInstance mOnyxInstance;
		GridRenderer mGridRenderer;
		SceneObject mSelectedSceneObject;
		Ray mClickRay;
		ObjectHandler mObjectHandler;
		OnyxViewerNavigation mNavigation = new OnyxViewerNavigation();

        // --------------------------------------------------------------------

        public MainWindow()
		{
			
			InitializeComponent();
            InitializeCanvas();

            Selection.OnSelectionChanged += OnSelectionChanged;
            SceneManagement.OnSceneChanged += OnSceneChanged;

            mNavigation.Bind(renderCanvas);

            KeyPreview = true;
		}

        // --------------------------------------------------------------------

        private void InitializeEditor()
		{
			Onyx3DEngine.InitMain(renderCanvas.Context, renderCanvas.WindowInfo);
			mOnyxInstance = Onyx3DEngine.Instance;

			mNavigation.CreateCamera();

			mObjectHandler = new ObjectHandler(mOnyxInstance, renderCanvas, mNavigation.Camera);
			mObjectHandler.OnTransformModified += OnTransformModifiedFromObjectHandler;

			selectedObjectInspector.InspectorChanged += OnInspectorChanged;

			SceneObject grid = new SceneObject("Grid");
			mGridRenderer = grid.AddComponent<GridRenderer>();
			mGridRenderer.GenerateGridMesh(100, 100, 0.25f, 0.25f, Vector3.One);
			mGridRenderer.Material = mOnyxInstance.Resources.GetMaterial(BuiltInMaterial.Unlit);
			mGridRenderer.Material.Properties["color"].Data = new Vector4(1, 1, 1, 0.1f);
		}
        
        // --------------------------------------------------------------------

        private void OnSelectionChanged(List<SceneObject> selected)
		{
			mSelectedSceneObject = Selection.ActiveObject;
			renderCanvas.Refresh();

			if (mSelectedSceneObject != null)
			{
				selectedObjectInspector.Fill(mSelectedSceneObject);
			}
			else
			{
				selectedObjectInspector.Fill(SceneManagement.ActiveScene);
			}

			mObjectHandler.HandleObject(mSelectedSceneObject);
			RenderScene();
		}

        // --------------------------------------------------------------------

        private void OnInspectorChanged(object sender, EventArgs args)
		{
			RenderScene();
		}

        // --------------------------------------------------------------------

        private void OnTransformModifiedFromObjectHandler()
		{
			selectedObjectInspector.Fill(mSelectedSceneObject);
		}

        // --------------------------------------------------------------------

        private void OnSceneChanged(Scene s)
		{
			sceneHierarchy.SetScene(s);
			renderCanvas.Refresh();
            Selection.ActiveObject = null;
		}

        // --------------------------------------------------------------------

        private void HighlightSelected()
		{
			if (mSelectedSceneObject != null)
			{
				mObjectHandler.Update();

				foreach (SceneObject obj in Selection.Selected)
				{
					Bounds bounds = obj.CalculateBounds();
					mOnyxInstance.Gizmos.DrawBox(bounds.Center, bounds.Size, Color.White.ToVector().Xyz);
				}

				Bounds activeBounds = Selection.ActiveObject.CalculateBounds();
				mOnyxInstance.Gizmos.DrawBox(activeBounds.Center, activeBounds.Size * 1.01f, Color.Orange.ToVector().Xyz);
			}

		}

        // --------------------------------------------------------------------

        private void RenderScene()
		{            
            if (!canDraw)
				return;
            
            renderCanvas.MakeCurrent();
            mOnyxInstance.Resources.RefreshAll();

            mNavigation.UpdateCamera();
            
            mOnyxInstance.Renderer.Render(SceneManagement.ActiveScene, mNavigation.Camera, renderCanvas.Width, renderCanvas.Height);
			mOnyxInstance.Renderer.Render(mGridRenderer, mNavigation.Camera);

			HighlightSelected();

			mOnyxInstance.Gizmos.DrawComponentGizmos(mNavigation.Camera, SceneManagement.ActiveScene);            

			renderCanvas.SwapBuffers();
		}

        // --------------------------------------------------------------------

        public void UpdateHierarchy()
        {
            sceneHierarchy.UpdateScene();
        }

        // --------------------------------------------------------------------

        #region RenderCanvas callbacks

        private void renderCanvas_Load(object sender, EventArgs e)
		{
			InitializeEditor();
            SceneManagement.LoadInitScene();

            canDraw = true;

			RenderScene();
		}

		private void renderCanvas_Paint(object sender, PaintEventArgs e)
		{
			RenderScene();
			renderInfoLabel.Text = string.Format("Render Time: {0}ms", Math.Round(mOnyxInstance.Renderer.RenderTime));
			labelLoggerOutput.Text = Logger.Instance.Content;
			Profiler.Instance.Clear();
		}

		private void renderCanvas_Click(object sender, EventArgs e)
		{
			MouseEventArgs mouseEvent = e as MouseEventArgs;

			if (mouseEvent.Button == MouseButtons.Left && !mObjectHandler.IsHandling)
			{
				mClickRay = mNavigation.Camera.ScreenPointToRay(mouseEvent.X, mouseEvent.Y, renderCanvas.Width, renderCanvas.Height);

				RaycastHit hit = new RaycastHit();
				if (Physics.RaycastScene(mClickRay, out hit, SceneManagement.ActiveScene))
				{
					if (ModifierKeys.HasFlag(Keys.Control))
					{
						Selection.Add(hit.Object);
					}
					else
					{
						Selection.ActiveObject = hit.Object;
					}
				}
				else
				{
					Selection.ActiveObject = null;
				}

				renderCanvas.Refresh();
			}
		}


        #endregion

        // --------------------------------------------------------------------

        #region UI callbacks

        private void timer1_Tick(object sender, EventArgs e)
		{
            //myTeapot.Transform.Rotate(new Vector3(0, 0.1f, 0));
            //renderCanvas.Refresh();
            //mReflectionProbe.Angle += 0.01f;
            mNavigation.OnFrameTick();
		}

		private void toolStripButtonEntityManager_Click(object sender, EventArgs e)
		{
			new EntitySelectorWindow().Show();
		}

		private void toolStripButtonSaveProject_Click(object sender, EventArgs e)
		{
            EditorSceneUtils.Save();
            ProjectLoader.Save();
		}

		private void toolStripButtonMaterials_Click(object sender, EventArgs e)
		{
            MaterialEditorWindow matEditor = new MaterialEditorWindow();
            matEditor.Show();
		}

		private void toolStripButtonTextures_Click(object sender, EventArgs e)
		{
			new TextureManagerWindow().Show();
		}

		private void toolStripButtonNewProject_Click(object sender, EventArgs e)
		{
			var confirmResult = MessageBox.Show("Are you sure to start a new project?", "New Project", MessageBoxButtons.YesNo);
			if (confirmResult == DialogResult.Yes)
			{
				ProjectManager.Instance.New();
                SceneManagement.New();
                ProjectLoader.Save();
            }
		}

		private void toolStripButtonOpenProject_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog1 = new OpenFileDialog();

			openFileDialog1.InitialDirectory = "c:\\";
			openFileDialog1.Filter = "Onyx3d project files (*.o3dproj)|*.o3dproj";
			openFileDialog1.FilterIndex = 2;
			openFileDialog1.RestoreDirectory = true;

			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				ProjectManager.Instance.Load(openFileDialog1.FileName);
                SceneManagement.LoadInitScene();
			}
		}

		private void toolStripButtonChangeScene_Click(object sender, EventArgs e)
		{
			SceneSelectorWindow ss = new SceneSelectorWindow();
			ss.OnSceneSelected += OnSceneChanged;
			ss.Show();
		}

		private void toolStripButtonOpenScene_Click(object sender, EventArgs e)
		{
            EditorSceneUtils.Open();
		}

		private void toolStripCreateCube_Click(object sender, EventArgs e)
		{
			EditorSceneObjectUtils.AddPrimitive(BuiltInMesh.Cube, "Cube");
		}

		private void toolStripCreateCylinder_Click(object sender, EventArgs e)
		{
            EditorSceneObjectUtils.AddPrimitive(BuiltInMesh.Cylinder, "Cylinder");
		}

		private void toolStripCreateTeapot_Click(object sender, EventArgs e)
		{
            EditorSceneObjectUtils.AddPrimitive(BuiltInMesh.Teapot, "Teapot");
		}

		private void toolStripCreateSphere_Click(object sender, EventArgs e)
		{
            EditorSceneObjectUtils.AddPrimitive(BuiltInMesh.Sphere, "Sphere");
		}

        private void toolStripCreateQuad_Click(object sender, EventArgs e)
        {
            EditorSceneObjectUtils.AddPrimitive(BuiltInMesh.Quad, "Quad");
        }

        private void toolStripCreateLight_Click(object sender, EventArgs e)
		{
            EditorSceneObjectUtils.AddLight();
        }

        private void toolStripCreateTemplate_Click(object sender, EventArgs e)
        {
            EditorEntityUtils.AddProxy();
        }

        private void toolStripCreateCamera_Click(object sender, EventArgs e)
        {
            EditorSceneObjectUtils.AddCamera();
        }

        private void toolStripButtonMove_Click(object sender, EventArgs e)
		{
			toolStripButtonScale.Checked = false;
			toolStripButtonMove.Checked = true;
			toolStripButtonRotate.Checked = false;
			mObjectHandler.SetAxisAction(ObjectHandler.HandlerAxisAction.Translate);
			renderCanvas.Refresh();
		}

		private void toolStripButtonScale_Click(object sender, EventArgs e)
		{
			toolStripButtonScale.Checked = true;
			toolStripButtonMove.Checked = false;
			toolStripButtonRotate.Checked = false;
			mObjectHandler.SetAxisAction(ObjectHandler.HandlerAxisAction.Scale);
			renderCanvas.Refresh();
		}

		private void toolStripButtonRotate_Click(object sender, EventArgs e)
		{
			toolStripButtonScale.Checked = false;
			toolStripButtonMove.Checked = false;
			toolStripButtonRotate.Checked = true;
			mObjectHandler.SetAxisAction(ObjectHandler.HandlerAxisAction.Rotate);
			renderCanvas.Refresh();
		}
        
		private void toolStripButtonImportModel_Click(object sender, EventArgs e)
		{
            if (ProjectManager.Instance.CurrentProjectPath.Length == 0)
            {
                ProjectLoader.Save();
            }
            else
            {
                new ModelImporterWindow().Show();
            }
		}

        private void duplicateSceneObjectToolStripMenuItem_Click(object sender, EventArgs e)
		{
            EditorSceneObjectUtils.Duplicate();
		}

		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Selection.ActiveObject != null)
                EditorSceneObjectUtils.Delete(Selection.Selected);
		}

		private void toolStripCreateReflectionProbe_Click(object sender, EventArgs e)
		{
            EditorSceneObjectUtils.AddReflectionProbe();
		}

		private void bakeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			renderCanvas.MakeCurrent();
			mOnyxInstance.Renderer.RefreshReflectionProbes();
			renderCanvas.Refresh();
		}

		private void setParentToolStripMenuItem_Click(object sender, EventArgs e)
		{
            EditorSceneObjectUtils.SetActiveAsParent();
		}
		

		private void clearParentToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
            EditorSceneObjectUtils.ClearParent();
		}

		private void groupObjectsToolStripMenuItem_Click(object sender, EventArgs e)
		{
            EditorSceneObjectUtils.Group(Selection.Selected);
		}

		private void createEntityToolStripMenuItem_Click(object sender, EventArgs e)
		{
            EditorEntityUtils.CreateFromSelection();
		}

		private void excludeFromEntityToolStripMenuItem_Click(object sender, EventArgs e)
		{
            EditorEntityUtils.ExcludeSelection();
		}

		private void saveSceneToolStripMenuItem_Click(object sender, EventArgs e)
		{
            EditorSceneUtils.Save();
		}

		private void newSceneToolStripMenuItem_Click(object sender, EventArgs e)
		{
            SceneManagement.New();
        }


        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int newAssets = ProjectManager.Instance.Content.RefreshAssets();
            if (newAssets > 0)
            {
                MessageBox.Show(string.Format("{0} new assets have been imported", newAssets));
            }
        }

        #endregion

    }
}
