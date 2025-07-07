import sqlite3

def initialise_db():
    with sqlite3.connect('leaderboard.db') as conn:
        with open('create_leaderboard.sql', 'r') as f:
            sql_script = f.read()
            conn.executescript(sql_script)
        print('Database initialised')
        
if __name__ == "__main__":
    initialise_db()