using System.Windows.Media.Media3D;
using System;

namespace ShoeCurveEditor
{
    class ModelObjViewer
    {
        private System.Windows.Controls.Viewport3D MainView;
        private ModelVisual3D Model;
        private Camera MainCamera;
        private Light MainLight;
        private System.Windows.Media.Color lcolor = System.Windows.Media.Colors.Green;
        private System.Windows.Media.Brush mcolor = System.Windows.Media.Brushes.Yellow;

        public System.Windows.Media.Brush ModelColor
        {
            get { return mcolor; }
            set { mcolor = value; }
        }

        public System.Windows.Media.Color LightColor
        {
            get { return lcolor; }
            set { lcolor = value; }
        }

        public Light ModelLight
        {
            get { return MainLight; }
            set { MainLight = value; }
        }
        
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="viewport">То, в чем рисуем</param>
        public ModelObjViewer(System.Windows.Controls.Viewport3D viewport)
        {
            MainView = viewport;
            MainCamera = new PerspectiveCamera(new Point3D(200, 150, 30), new Vector3D(-20, -50, 0),
                new Vector3D(0, 0, -1), 90);
            MainLight = new DirectionalLight(lcolor, new Vector3D(-200, -150, -30));
        }

        /// <summary>
        /// Этим подписываемся на необходимость перерисовки, когда точки поменялись.
        /// </summary>
        /// <param name="sender">Кто уведомляет</param>
        /// <param name="e"><c>obsolete</c>Параметры</param>
        public void Update3DPicture(object sender, EventArgs e)
        {
            ModelLogicInteractor Logic = sender as ModelLogicInteractor;
            LoadViewData(Logic.GetFaces(), Logic.GetPoints());
        }

        /// <summary>
        ///  Грузит модель и заполняет всё необходнимое на сцене.
        /// </summary>
        /// <param name="Data">Массив треугольников</param>
        /// <param name="Vertexes">Вершины</param>
        public void LoadViewData(ModelLogicInteractor.Triangle[] Data, Vector3D[] Vertexes)
        {
            MainView.Camera = MainCamera;
            MainView.Children.Clear();
            FillModelData(Data, Vertexes);
            MainView.Children.Add(Model);
        }

        private void FillModelData(ModelLogicInteractor.Triangle[] Data, Vector3D[] Vertexes)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            foreach (Point3D pt in Vertexes) mesh.Positions.Add(pt);
            foreach (ModelLogicInteractor.Triangle tr in Data)
            {
                mesh.TriangleIndices.Add(tr.V[0]);
                mesh.TriangleIndices.Add(tr.V[1]);
                mesh.TriangleIndices.Add(tr.V[2]);
                mesh.Normals.Add(tr.norm);
            }
            Model = new ModelVisual3D();
            Model3DGroup gr = new Model3DGroup();
            GeometryModel3D gm = new GeometryModel3D();
            gm.Geometry = mesh;
            gm.Material = new DiffuseMaterial(System.Windows.Media.Brushes.Green);
            gm.BackMaterial = new DiffuseMaterial(System.Windows.Media.Brushes.LightGreen);
            gr.Children.Add(gm);
          //  gr.Chilren.Add(new GeometryModel3D(mesh, new DiffuseMaterial(mcolor) { AmbientColor = System.Windows.Media.Colors.Orange, Color = System.Windows.Media.Colors.Orange }));
            gr.Children.Add(MainLight);
            gr.Children.Add(new AmbientLight(lcolor));
            Model.Content = gr;
        }
    }
}