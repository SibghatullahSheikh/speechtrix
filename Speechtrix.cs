﻿using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Diagnostics;
using System.Drawing;


namespace Speechtrix
{
    public class Speechtrix
    {
        const int maxY = 32;
        const int maxX = 64;
		const int nextLevelLines = 20;
        Color[] cola = new Color[7]
            {Color.FromArgb(226, 0, 127), Color.FromArgb(255, 0, 0), Color.FromArgb(0, 255, 0), 
                Color.FromArgb(12, 0, 247), Color.FromArgb(255, 240, 0), Color.FromArgb(122, 78, 156),
                Color.FromArgb(45, 237, 120)};

        short[][,] sizes = new short[2][,]
        {
            new short[7,4]{{2,3,2,3},{1,4,1,4},{2,2,2,2},{2,3,2,3},{2,3,2,3},{2,3,2,3},{2,3,2,3}},
            new short[7,4]{{3,2,3,2},{4,1,4,1},{2,2,2,2},{3,2,3,2},{3,2,3,2},{3,2,3,2},{3,2,3,2}}
        };
        bool newBlock;
        bool[,] gamefield; //matris med positioner, där vi använder 16(?) som standardbredd (position (0,0) är högst upp i vänstra hörnet)
        int speed; //hastighet för fallande block i ms(?)
        int level; //kanske för reglera speed/hur mycket poäng man får/vilka block som kommer
        int score;
		int linesToNextLevel; //lines needed to be removed until reaching next level
		int startX, startY, endX, endY, height, width;
		Graphics g;
        Thread tt;
        Thread graphicsThread;
        LogicBlock current, next;
        Random rand;

        public Speechtrix()
        {
            rand = new Random();
            gamefield = new bool[maxX,maxY];
			linesToNextLevel = nextLevelLines;

			startY = 0; startX = 0;
			height = 20; width = 10;
			endY = height; endX = width;
            Debug.Print("creating speechtrix");
            g = new Graphics(width, height, this);
			graphicsThread = new Thread(new ThreadStart(Graphics.Run));
            graphicsThread.Start();

            Thread.Sleep(10000);

            Debug.Print("created graphics");
        }

        public static void Main(string[] args)
        {
	//		Console.WriteLine("Would you like to play?");
	//		if (Console.Read() == "y")
		//	{
				Speechtrix sp = new Speechtrix();
                
				sp.newGame();
                sp.play();

                
		//	}
        }
        void newGame() //nollställer tid, score, level, tömmer matris gamefield
        {
            for (int x = startX; x < endX; x++)
                for (int y = startY; y < endY; y++)
                    gamefield[x, y] = false;

            speed = 400;
            level = 1;
            score = 0;
            linesToNextLevel = nextLevelLines;

            TimerThread timer = new TimerThread(ref g);
            tt = new Thread(new ThreadStart(timer.Run));
            tt.Start();
            updateScore(2523);
        }

        void copyNextToCurrent()
        {
            current.nr = next.nr;
            current.rot = next.rot;
            current.blo = Blocks.getRotations(current.nr, next.rot);
            current.bxs = next.bxs;
            current.bys = next.bys;
            current.x = next.x;
            current.y = next.y;
            current.color = next.color;
        }
        void initiateNewNext()
        {
            next.nr = (short)rand.Next(0, 7);
            next.rot = (short)rand.Next(0, 4);
            next.blo = Blocks.getRotations(next.nr, next.rot);
            next.bxs = sizes[0][next.nr, next.rot];
            next.bys = sizes[1][next.nr, next.rot];
            next.y = 0;
            next.x = (short)(width / 2 - next.bxs / 2);
            next.color = cola[next.nr];
        }



        /*************************************************/
        void play()
        {
            current = new LogicBlock();
            next = new LogicBlock();

            initiateNewNext();

            newBlock = true;

            for (short i=0; !checkEnd(); i++)
            {
                if (newBlock)
                {
                    newBlock = false;
                    copyNextToCurrent();
                    initiateNewNext();
                    g.setNext(next);
                    Debug.Print("Sizes: " + current.bxs + " " + current.bys);
                }

				g.setBlock(current); //update falling block graphically

                if (checkRowBelow())
                {
                    lockBlock();
                    checkFullLine();
                }
                else
                    current.y++; //falling

                Thread.Sleep(speed);
            }
            endGame();
        }
        /*************************************************/

        void lockBlock()
        {
            for (int x = 0; x < current.bxs; x++)
            { 
                for (int y = 0; y < current.bys; y++)
                {
                    if (current.blo[x, y])
                    {
                        gamefield[current.x + x, current.y + y] = true;
                    }
                }
            }
			/*for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					if (gamefield[j, i])
						Debug.Print(""+1);
					else
						Debug.Print(""+0);
				}
			}*/
            g.lockBlock(current, next);
            newBlock = true;
        }

		int[][] getRealCoord()
		{
			int[][] co = new int[4][];
			int count = 0;
			for (int x=0; x < current.bxs; x++)
			{
				for (int y=0; y < current.bys; y++)
				{
					if (current.blo[x,y])
					{
						co[count] = new int[2]{
						    x+current.x,
						    y+current.y
                        };
						count++;
					}
				}
			}
			return co;
		}

        bool checkRowBelow()
        {
			Debug.Print("\ncheckrow");
            int[][] coord = new int[4][] { new int[2] { 0, 0 }, new int[2] { 0, 0 }, new int[2] { 0, 0 }, new int[2] { 0, 0 } };
            coord = getRealCoord();

			for (int i = 0; i < 4; i++) //gå igenom blockets 4 (x,y)-koordinater
                if (gamefield[coord[i][0], coord[i][1]+1] //om det på raden under en koordinat finns sparat block
                    || coord[i][1] == height-1) //om y-koordinat är på sista raden
					return true;

            return false;
        }

        void newLevel() //ändra level -> ändrar speed/poängsätt...
        {
            level++;
			linesToNextLevel = nextLevelLines;
			if (speed > 100)
				setSpeed(speed-nextLevelLines);
        }
		void setSpeed(int ms) //sätter fallhastighet
		{
			speed = ms;
		}

        bool checkEnd() //när ett block inte får plats i gamefield
		{
            bool end = false;



            return end;
		}
        void endGame() //när spelet tar slut
		{
            tt.Abort();
            checkHighscore();
            showMenu();
		}
        void checkHighscore()
        {

        }
        void showMenu()
        {

        }
        void updateScore(int lines) //hur många lines som tagit bort
		{
			linesToNextLevel -= lines;
			if (linesToNextLevel <= 0)
				linesToNextLevel += nextLevelLines;

			score += level*10*lines;
            Debug.Print("POints:  " + score);
            g.setScore(score);
		}
        void checkFullLine() //kolla om det finns några rader i matrisen där alla är true, isf anropa deleteLine för varje rad
		{
            // ingen poäng med att kolla alla rader, man behöver bara current-blocks rader eftersom att det bara är de som kan ha blivit fulla
			int deletedLines = 0;
            short minY = current.y;
            short maxY = current.bys;
			//for (int i=startY; i<endY; i++)
            for (int i = minY; i < maxY; i++)
			{
				int j = startX;
				for (; j<maxX && !gamefield[j,i]; j++) ; //as long as gamefield is all true on a row
				if (j==maxX) //if whole row true
				{
					deletedLines++;
					deleteLine(i);
					g.removeRow(i);
				}
			}
			updateScore(deletedLines);
		}
		void deleteLine(int lineNumber) //i matrisen, flytta alla bool-värden från rader ovanför lineNumber en rad nedåt, anropar updateScore
		{
			for (int y = lineNumber; y > startY; y--)
			{
				for (int x = startX; x < endX; x++)
				{
                    gamefield[x, y] = gamefield[x, y - 1];
				}
			}
            g.removeRow(lineNumber);
		}
        public void keyUp()
        {
            current.rot = (short)((current.rot++) % 4);

            current.blo = Blocks.getRotations(current.nr, current.rot);
            

        }
        public void keyDown()
        {

        }
        public void keyLeft()
        {
                if (current.x > 0)
                {
                    current.x--;
                    g.setBlock(current); // makes the graphics bug a bit
                }
        }
        public void keyRight()
        {
            if (current.x < width - current.bxs) // should probably be changed to depending on the width of the block
            {
                current.x++;
                g.setBlock(current); // makes the graphics bug a bit
            }
        }
    }
    class TimerThread
    {
        Graphics g;
        int hours;
        int minutes;
        int seconds;

        public TimerThread(ref Graphics _g)
        {

            Debug.Print("creating TimerThread");
            g = _g;
            hours = 0;
            minutes = 0;
            seconds = 0;
        }
        public void Run()
        {
            while (true)
            {
                Thread.Sleep(1000);
                seconds++;
                if (seconds == 60)
                {
                    minutes++;
                    seconds = 0;
                }
                if (minutes == 60)
                {
                    hours++;
                    minutes = 0;
                }
                g.setTime(hours, minutes, seconds);
            }
        }

    }
}