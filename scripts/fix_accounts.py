import psycopg2
import bcrypt

conn_str = "postgresql://neondb_owner:npg_VPln5gFhfEw4@ep-hidden-math-awrpifwn-pooler.c-12.us-east-1.aws.neon.tech/neondb?sslmode=require"

try:
    conn = psycopg2.connect(conn_str)
    cur = conn.cursor()
    cur.execute('SELECT "Id", "TenDangNhap", "Role", "MatKhauHash" FROM "NguoiDung";')
    users = cur.fetchall()
    print(f"Users found in DB ({len(users)}):")
    for u in users:
        print(f"Id={u[0]}, TenDangNhap={u[1]}, Role={u[2]}, Hash={u[3]}")
    
    # Update or insert standard accounts
    accounts = [
        ("admin", "admin123", "Admin", "Quản trị viên hệ thống"),
        ("troly", "troly123", "TroLyGiaoVu", "Trần Thị Bích Trợ Lý"),
        ("giaovu", "giaovu123", "GiaoVuBoMon", "Lê Văn Giáo Vụ"),
        ("giangvien", "giangvien123", "GiangVien", "Nguyễn Văn An")
    ]
    
    for username, password, role, fullname in accounts:
        # Generate BCrypt hash
        salt = bcrypt.gensalt(rounds=11)
        hashed = bcrypt.hashpw(password.encode('utf-8'), salt).decode('utf-8')
        
        # Check if user exists
        cur.execute('SELECT "Id" FROM "NguoiDung" WHERE LOWER("TenDangNhap") = LOWER(%s)', (username,))
        existing = cur.fetchone()
        if existing:
            cur.execute('''
                UPDATE "NguoiDung"
                SET "MatKhauHash" = %s, "Role" = %s, "HoTen" = %s, "TrangThai" = 'HoatDong', "IsDeleted" = false
                WHERE "Id" = %s
            ''', (hashed, role, fullname, existing[0]))
            print(f"Updated user {username} with password '{password}'")
        else:
            cur.execute('''
                INSERT INTO "NguoiDung" ("TenDangNhap", "MatKhauHash", "HoTen", "Role", "TrangThai", "IsDeleted", "NgayTao")
                VALUES (%s, %s, %s, %s, 'HoatDong', false, NOW())
            ''', (username, hashed, fullname, role))
            print(f"Inserted user {username} with password '{password}'")
            
    conn.commit()
    print("All accounts seeded/updated successfully!")
    cur.close()
    conn.close()
except Exception as ex:
    print(f"Error: {ex}")
