using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tensorflow;
using static Tensorflow.Binding;

namespace DWSIM.UnitOperations.NeuralNetwork.Classes
{
    class Utils
    {

        public static string SaveGraphToZip(Session session, ANNModel model, string zippath = null)
        {

            var tempdir = CreateUniqueTempDirectory();

            session.graph.as_default();

            tf.train.Saver().save(session, Path.Combine(tempdir, model.ModelName));

            if (zippath == null) zippath = Path.GetTempFileName();

            ZipFile.CreateFromDirectory(tempdir, zippath);

            Directory.Delete(tempdir, true);

            return zippath;

        }

        public static bool SaveGraphToZipStream(Session session, ANNModel model, MemoryStream stream)
        {

            var tempdir = CreateUniqueTempDirectory();

            session.graph.as_default();

            tf.train.Saver().save(session, Path.Combine(tempdir, model.ModelName));

            var zippath = Path.GetTempFileName();

            File.Delete(zippath);

            ZipFile.CreateFromDirectory(tempdir, zippath);

            stream.Position = 0;
            using (FileStream file = new FileStream(zippath, FileMode.Open, FileAccess.Read))
                file.CopyTo(stream);

            Directory.Delete(tempdir, true);

            return true;

        }

        public static Session LoadGraphFromZip(string zippath)
        {

            var sess = tf.Session();

            var tempdir = CreateUniqueTempDirectory();

            ZipFile.ExtractToDirectory(zippath, tempdir);

            var metafile = Directory.GetFiles(tempdir, "*.meta")[0];

            var loader = tf.train.import_meta_graph(metafile);
            loader.restore(sess, tf.train.latest_checkpoint(tempdir));

            Directory.Delete(tempdir, true);

            return sess;

        }

        public static Session LoadGraphFromStream(MemoryStream stream)
        {

            var sess = tf.Session();

            var tempdir = CreateUniqueTempDirectory();

            var tempfile = Path.GetTempFileName();

            stream.Position = 0;

            using (FileStream file = new FileStream(tempfile, FileMode.Create, System.IO.FileAccess.Write))
                stream.CopyTo(file);

            ZipFile.ExtractToDirectory(tempfile, tempdir);

            var metafile = Directory.GetFiles(tempdir, "*.meta")[0];

            var loader = tf.train.import_meta_graph(metafile);
            loader.restore(sess, tf.train.latest_checkpoint(tempdir));

            File.Delete(tempfile);
            Directory.Delete(tempdir, true);

            return sess;

        }

        public static string CreateUniqueTempDirectory()
        {
            var uniqueTempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            Directory.CreateDirectory(uniqueTempDir);
            return uniqueTempDir;
        }

        public static string StreamToBase64(MemoryStream ms)
        {
            var bytes = ms.ToArray();
            var base64String = Convert.ToBase64String(bytes);
            return base64String;
        }

        public static MemoryStream Base64ToStream(string data)
        {
            var bytes = Convert.FromBase64String(data);
            var ms = new MemoryStream(bytes, 0, bytes.Length);
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;
            return ms;
        }

        public static float Scale(float value, float min, float max, float minScale, float maxScale)
        {
            float scaled = minScale + (value - min) / (max - min) * (maxScale - minScale);
            if (double.IsNaN(scaled) || double.IsInfinity(scaled)) scaled = minScale;
            return scaled;
        }

        public static float UnScale(float scaledvalue, float min, float max, float minScale, float maxScale)
        {
            float unscaled = min + (scaledvalue - minScale) * (max - min) / (maxScale - minScale);
            if (double.IsNaN(unscaled) || double.IsInfinity(unscaled)) unscaled = min;
            return unscaled;
        }

    }

}
