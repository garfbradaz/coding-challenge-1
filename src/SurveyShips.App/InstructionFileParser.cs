using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace SurveyShips.App
{

/// <summary>
/// Simple Object to Parse the Instruction file.
/// </summary>
public class InstructionFileParser
    {
        private static List<string> parsed;

        /// <summary>
        /// Parse the file, and validate the Instructions.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Validated instructions.</returns>
        public static async Task<List<string>> ParseLinesAsync(string fileName)
        {
            string line = default(string);
            int count = 1;
            bool firstRecordPassed  = false;
            parsed = new List<string>();
            try
            {
                using (StreamReader sr = new StreamReader(fileName))
                {
                    while((line = await sr.ReadLineAsync()) != null)  
                    {  
                        if(string.IsNullOrWhiteSpace(line))
                        {
                                count = 1; //reset back
                                continue;
                        }
                        if(!firstRecordPassed)
                        {
                            if( ! await parseFirstRecord(line))
                            {
                                throw new IOException("First line failed"); //Throw because without this you cannot set grid.
                            }
                            firstRecordPassed = true;
                            parsed.Add(line);
                            continue;
                        }
                    

                        
                        if(count == 1)                                      //Ship Position
                        {
                            if(await parseShipCoordinates(line))
                            {
                                parsed.Add($"[ship-start] {line}");
                            }
                            else 
                            {
                                parsed.Add($"[ship-start] Failed");          //Added Failed record so that the calling application can still continue
                            }
                            count++;
                            continue;
                        } else if (count == 2)                              //Ship Instructions
                        {
                            if(await parseShipInstructions(line))
                            {
                                parsed.Add($"[ship-instr] {line}");
                            }else
                            {
                                parsed.Add($"[ship-instr] Failed");
                            }
                            count = 1;
                        }
                    }  
                    sr.Close();
                }
        
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                Console.WriteLine($"Issue opening file: {ex}");
            }
            return parsed;
        }
    
        /// <summary>
        /// First record should only contain 2 numbers to build the size of the
        /// rectangle
        /// </summary>
        /// <param name="record"></param>
        /// <returns>true: parsed correctly, false: failed.</returns>
        private static async Task<bool> parseFirstRecord(string record)
        {
            if(string.IsNullOrEmpty(record))
                return false;
            char[] toCheck = new char[record.Length];
            using(var reader = new StringReader(record))
            {
                await reader.ReadAsync(toCheck, 0, record.Length);
                foreach(var c in toCheck)
                {
                    if(char.IsWhiteSpace(c))
                        continue;
                    if(!char.IsNumber(c))
                        return false;
                }
                reader.Close();
            }
            return true;
        }

        /// <summary>
        /// The record that represents the Coordinates and Orientation
        /// should only contain numbers not greater than 50 and N,S,E,W
        /// </summary>
        /// <param name="record"></param>
        /// <returns>true: parsed correctly, false: failed.</returns>
        private static async Task<bool> parseShipCoordinates(string record)
        {
            if(string.IsNullOrEmpty(record))
                return false;
            char[] toCheck = new char[record.Length];
            using(var reader = new StringReader(record))
            {
                await reader.ReadAsync(toCheck, 0, record.Length);
                foreach(var c in toCheck)
                {
                    if(char.IsWhiteSpace(c))
                        continue;
                    if(char.IsNumber(c))
                    {
                        var parsedInt = (int)char.GetNumericValue(c);
                        if(parsedInt > 50)
                        {
                            return false;
                        }
                    }
                    if(char.IsLetter(c))
                    {
                        var lower = char.ToLowerInvariant(c);
                        switch(lower)
                        {
                            case 'n':
                            case 'e':
                            case 's':
                            case 'w':
                                break;
                            default:
                                return false;
                        }
                    }
                }
                reader.Close();
            }
            return true;         
        }
        
        /// <summary>
        /// The record that represents the Instructions for movement, should only
        /// contain (L)eft, (R)ight and (F)orward. Also the instruction should be less than
        ///  100 characters in length.
        /// </summary>
        /// <param name="record"></param>
        /// <returns>true: parsed correctly, false: failed.</returns>
        private static async Task<bool> parseShipInstructions(string record)
        {
            if(string.IsNullOrEmpty(record))
                return false;
            char[] toCheck = new char[record.Length];
            using(var reader = new StringReader(record))
            {
                if(record.Count() > 99) 
                {
                    return false;
                }               
                await reader.ReadAsync(toCheck, 0, record.Length);
                foreach(var c in toCheck)
                {
                    if(char.IsWhiteSpace(c))
                        continue;
                    if(char.IsLetter(c))
                    {
                        var lower = char.ToLowerInvariant(c);
                        switch(lower)
                        {
                            case 'l':
                            case 'f':
                            case 'r':
                                break;
                            default:
                                return false;
                        }
                    }
                }
                reader.Close();
            }
            return true;         
        }

        /// <summary>
        /// Total successfully parsed ships ()
        /// </summary>
        /// <returns></returns>
        public static int CountOfShips()
        {
            return parsed.Where(s => s.Contains("[ship-start]") && !s.Contains("Failed")).Count();
        }

   
        /// <summary>
        /// Returns the Grid coordinates.
        /// </summary>
        /// <returns>X, Y coordinates</returns>
        public static (int x, int y) GridCoordinates()
        {
            string value = parsed[0];
            if(string.IsNullOrWhiteSpace(value))
                return (0,0);

            int x , y = 0;
            char xx = value[0];
            char yy = value[2];
            
            x = (int)char.GetNumericValue(xx);
            y = (int)char.GetNumericValue(yy);

            return (x,y);
        }   
    
         /// <summary>
        /// Get Starting Coordinates and Orientation
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetShipStartCoordinates()
        {
            return parsed.Where(s => s.Contains("[ship-start]") && !s.Contains("Failed"));
                         
        }

        /// <summary>
        /// Get Starting Coordinates and Orientation
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="orientation"></param>
        /// <returns></returns>
        public static List<(int,int,char)> GetShipStartCoordinatesAsList()
        {
            //[ship-start] 
            var coords = GetShipStartCoordinates().Select(i => i.Substring(13,i.Length-13)).ToList();
            if(coords?.Count() == 0)
            {
                return new List<(int, int, char)> {
                    (0,0,'U')
                };
            }
            var converted = new List<(int,int,char)>();

            foreach(var coord in coords)
            {               
                int x , y = 0;
                char xx = coord[0];
                char yy = coord[2];
                char zz = coord[4];
            
                x = (int)char.GetNumericValue(xx);
                y = (int)char.GetNumericValue(yy);

                (int,int,char) item = (x,y,zz);
                converted.Add(item);
            }
            return converted;
        }

        /// <summary>
        /// Get Ship Instructions
        /// </summary>
        /// <returns></returns>
        public static List<string> GetShipInstructions()
        {
            return parsed.Where(s => s.Contains("[ship-instr]") && !s.Contains("Failed"))
                         .Select(i => i.Substring(13,i.Length-13)).ToList();
        }
    }
}