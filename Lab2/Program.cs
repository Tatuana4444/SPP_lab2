using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;


namespace Lab1
{
    delegate void TaskDelegate();

    class Program
    {
        static object locker = new object();
        public static Queue<string> QueueFile = new Queue<string>();
        public static Queue<string> QueueEndDir = new Queue<string>();
        public static Queue<string> QueueFileWithoutPath = new Queue<string>();
        public static int CountFile = 0;
        public static void EndProgram()
        {
            Console.WriteLine("Количество скопированных файлов "+ CountFile.ToString());
        }

        public static void CopyFile()
        {
            string file, end_dir, filik;
            lock (locker) {
                file = QueueFile.Dequeue();//Вынимаем из очереди данные
                end_dir = QueueEndDir.Dequeue();
                filik = QueueFileWithoutPath.Dequeue();
            }
                File.Copy(file, end_dir + "\\" + filik, true);//Копируем
        }

        static void perebor_updates(string BeginDir, string EndDir, TaskQueue Task)
        {
            
            DirectoryInfo dir_inf = new DirectoryInfo(BeginDir);//Берём нашу исходную папку
            
            foreach (DirectoryInfo dir in dir_inf.GetDirectories())//Перебираем все внутренние папки
            {                
                if (Directory.Exists(EndDir + "\\" + dir.Name) != true)//Проверяем - если директории не существует, то создаём;
                {
                    Directory.CreateDirectory(EndDir + "\\" + dir.Name);
                }                
                perebor_updates(dir.FullName, EndDir + "\\" + dir.Name, Task);//Перебираем вложенные папки и делаем для них то-же самое
            }                       
            foreach (string File in Directory.GetFiles(BeginDir))//Перебираем файлики в папке источнике.
            {               
                string FileWithoutPath = File.Substring(File.LastIndexOf('\\'), File.Length - File.LastIndexOf('\\')); //Определяемимя файла с расширением - без пути
                QueueFile.Enqueue(File);//Добавляем в очередь пути, имя файла и задачу на выполнение
                QueueEndDir.Enqueue(EndDir);
                QueueFileWithoutPath.Enqueue(FileWithoutPath);
                Task.EnqueueTask(CopyFile);
                CountFile++;//Счетчик файлов
               
            }
        }
        //добавить очередь для имен файл и в по-одному их потом выгружатьы
        static void Main(string[] args)
        {
            Console.WriteLine("Введите количество потоков");
            int Count = int.Parse(Console.ReadLine());
            TaskQueue task = new TaskQueue(Count);
            Console.WriteLine("Введите путь к исходному каталогу");
            string BeginDir = Console.ReadLine();//"C:/Users/Татьяна/Desktop/Картинки"
            Console.WriteLine("Введите путь к целевому каталогу");
            string EndDir = Console.ReadLine();//"C:/3курс/3курс1сем/СПП/Лабы/Лаба2/Тест"
            perebor_updates(BeginDir, EndDir, task);
             task.EnqueueTask(EndProgram);
            task.IsStop = true;
            Console.ReadKey();
        }
    }

    class TaskQueue
    {

        static object locker = new object();
        static Queue<TaskDelegate> QTask;
        private Thread[] threads;
        private volatile bool isStop = false;
        public bool IsStop
        {
            get { return isStop; }
            set { isStop = value; }
        }

        public TaskQueue(int threadCount)//создаем пул потоков
        {
            QTask = new Queue<TaskDelegate>();
            threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                Thread thread = new Thread(Check);
                //  threads[i] = thread;
                thread.Start();
            }

        }
        public bool EnqueueTask(TaskDelegate task)
        {
            if (isStop)
                return false;
            QTask.Enqueue(task);
            return true;
        }


        public void Check()//проверяем, есть ли в очереди задачи
        {
            while (!isStop || (QTask.Count > 0))
            {//если еще не было конца или очередь не пуста
                TaskDelegate task = null;
                lock (locker)//блокировка, на время обращения к очереди
                {
                    if (QTask.Count > 0)
                        task = QTask.Dequeue();
                }
                task?.Invoke();//вызов задачи 
            }

        }
    }
}

