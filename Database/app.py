from flask import Flask, request, jsonify
import os
import psycopg2
import psycopg2.extras # For dict-like row access

app = Flask(__name__)

# Get DATABASE_URL from Railway's environment
DATABASE_URL = os.getenv("DATABASE_URL")

def get_db_connection():
    '''
    Helper to get database connection
    '''
    if not DATABASE_URL:
        raise RuntimeError("DATABASE_URL not set")

    # Connect with dict cursor so rows behave like sqlite3.Row
    return psycopg2.connect(DATABASE_URL, cursor_factory=psycopg2.extras.RealDictCursor)    

# POST /submit_score
@app.route('/submit_score', methods=['POST'])
def submit_score():
    data = request.get_json()
    
    if not data or 'player_name' not in data or 'score' not in data or 'level_name' not in data:
        return jsonify({'error': 'Missing player_name, score, or level_name'}), 400
    
    name = str(data['player_name']).strip()
    score = float(data['score'])
    level_name = str(data['level_name']).strip()
    
    try:
        conn = get_db_connection()
        cur = conn.cursor()
        cur.execute(
            "INSERT INTO leaderboard (player_name, score, level_name) VALUES (%s, %s, %s)",
            (name, score, level_name)
        )
        conn.commit()
        cur.close()
        conn.close()
        return jsonify({'message': 'Score submitted'}), 201
    except Exception as e:
        return jsonify({'error': str(e)}), 500
    
    return jsonify({'message': 'Score submitted;'}), 201

# GET /top_scores
@app.route('/top_scores', methods = ['GET'])
def top_scores():
    level_name = request.args.get('level_name')
    
    try:
        conn = get_db_connection()
        
        if level_name:
            query = "SELECT player_name, score, created_at FROM leaderboard WHERE level_name = ? ORDER BY score ASC LIMIT 10"
            rows = conn.execute(query, (level_name,)).fetchall()
        else:        
            rows = conn.execute("SELECT player_name, score, created_at FROM leaderboard ORDER BY score ASC LIMIT 10").fetchall()
        
        conn.close()
        
        # Convert rows to dicts
        scores = [
            {
                'player_name': row['player_name'], 
                'score': row['score'],
                'created_at': row['created_at']
                } 
                for row in rows]
        return jsonify(scores)
    except Exception as e:
        return jsonify({'error': str(e)}), 500
        

# Health check
@app.route("/status", methods=["GET"])
def status():
    return jsonify({"status": "ok"}), 200

if __name__==('__main__'):
    app.run(debug = True)
    